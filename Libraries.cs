using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LocalIconStudio {
class LibraryIcon {public string Id,Name,Prompt;public int MaxSize=256;public DateTime Created=DateTime.Now;}
class IconLibrary {public int Version=1;public string Id,Name;public List<LibraryIcon> Icons=new List<LibraryIcon>();}
static class LibraryStore {
    public static string Root=Path.Combine(Program.Data,"libraries");
    static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=1024*1024};}
    static void Id(string id){Guid value;if(!Guid.TryParseExact(id,"N",out value))throw new IOException(T("Ungültige Library-ID.","Invalid library ID."));}
    public static string T(string de,string en){return L.English?en:de;}
    public static string Folder(IconLibrary set){Id(set.Id);return Path.Combine(Root,set.Id);}
    public static string ImagePath(IconLibrary set,LibraryIcon icon){Id(icon.Id);return Path.Combine(Folder(set),"icons",icon.Id+".png");}
    public static void Validate(IconLibrary set){if(set==null||set.Version!=1||String.IsNullOrWhiteSpace(set.Name)||set.Name.Length>100||set.Icons==null||set.Icons.Count>300)throw new IOException(T("Ungültiges oder nicht unterstütztes Icon-Set.","Invalid or unsupported icon set."));Id(set.Id);var ids=new HashSet<string>();foreach(var icon in set.Icons){if(icon==null)throw new IOException("Invalid icon");Id(icon.Id);if(!ids.Add(icon.Id)||String.IsNullOrWhiteSpace(icon.Name)||icon.Name.Length>100||(icon.Prompt??"").Length>8000||!Ico.Sizes.Contains(icon.MaxSize))throw new IOException(T("Ungültiger Icon-Eintrag.","Invalid icon entry."));}}
    public static List<IconLibrary> Read(){var sets=new List<IconLibrary>();if(!Directory.Exists(Root))return sets;foreach(var folder in Directory.GetDirectories(Root)){Guid id;if(!Guid.TryParseExact(Path.GetFileName(folder),"N",out id))continue;var path=Path.Combine(folder,"library.json");if(!File.Exists(path))continue;var set=Json().Deserialize<IconLibrary>(File.ReadAllText(path));Validate(set);if(set.Id!=Path.GetFileName(folder))throw new IOException("Library folder mismatch");sets.Add(set);}return sets.OrderBy(s=>s.Name).ToList();}
    public static void Save(IconLibrary set){Validate(set);string folder=Folder(set);Directory.CreateDirectory(folder);string path=Path.Combine(folder,"library.json"),temp=path+".tmp";try{File.WriteAllText(temp,Json().Serialize(set),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
    public static IconLibrary Create(string name){var set=new IconLibrary{Id=Guid.NewGuid().ToString("N"),Name=name.Trim()};Save(set);return set;}
    public static LibraryIcon Add(IconLibrary set,Image image,string name,string prompt,int maxSize){var icon=new LibraryIcon{Id=Guid.NewGuid().ToString("N"),Name=name.Trim(),Prompt=prompt??"",MaxSize=maxSize};if(image.Width>8192||image.Height>8192)throw new IOException("Image too large");set.Icons.Add(icon);try{Validate(set);}catch{set.Icons.Remove(icon);throw;}string png=ImagePath(set,icon),ico=Path.ChangeExtension(png,".ico");Directory.CreateDirectory(Path.GetDirectoryName(png));try{image.Save(png,ImageFormat.Png);Ico.Write(image,ico,Ico.Sizes.Where(s=>s<=maxSize).ToArray());Save(set);return icon;}catch{set.Icons.Remove(icon);if(File.Exists(png))File.Delete(png);if(File.Exists(ico))File.Delete(ico);throw;}}
    public static List<HistoryEntry> Entries(IconLibrary set){return set.Icons.Select(i=>new HistoryEntry{ImagePath=ImagePath(set,i),Prompt=i.Prompt,Name=i.Name,Created=i.Created,MaxSize=i.MaxSize,Job=Folder(set)}).ToList();}
    public static string EntryName(HistoryEntry entry){string name=String.IsNullOrWhiteSpace(entry.Name)?entry.Prompt:entry.Name;if(String.IsNullOrWhiteSpace(name))name=T("Mein Icon","My icon");name=name.Replace("\r"," ").Replace("\n"," ").Trim();return name.Substring(0,Math.Min(100,name.Length));}
    public static void CopyMany(IconLibrary set,IEnumerable<HistoryEntry> entries,int fallbackSize){
        var chosen=entries.ToList();if(chosen.Count==0)return;
        var updated=new IconLibrary{Id=set.Id,Name=set.Name,Version=set.Version,Icons=set.Icons.ToList()};
        var added=chosen.Select(e=>new LibraryIcon{Id=Guid.NewGuid().ToString("N"),Name=EntryName(e),Prompt=e.Prompt??"",MaxSize=Ico.Sizes.Contains(e.MaxSize)?e.MaxSize:fallbackSize,Created=e.Created}).ToList();
        updated.Icons.AddRange(added);Validate(updated);var written=new List<string>();
        try{
            for(int i=0;i<chosen.Count;i++)using(var image=Image.FromFile(chosen[i].ImagePath)){
                if(image.Width>8192||image.Height>8192)throw new IOException("Image too large");
                string png=ImagePath(updated,added[i]),ico=Path.ChangeExtension(png,".ico");Directory.CreateDirectory(Path.GetDirectoryName(png));
                written.Add(png);written.Add(ico);image.Save(png,ImageFormat.Png);Ico.Write(image,ico,Ico.Sizes.Where(s=>s<=added[i].MaxSize));
            }
            // Publish the entire batch with one atomic manifest update.
            Save(updated);set.Icons=updated.Icons;
        }catch{foreach(string file in written)if(File.Exists(file))File.Delete(file);throw;}
    }
    public static IconLibrary CreateWithIcons(string name,IEnumerable<HistoryEntry> entries,int fallbackSize){
        var set=new IconLibrary{Id=Guid.NewGuid().ToString("N"),Name=name.Trim()};var chosen=entries.ToList();
        if(chosen.Count==0)Save(set);else CopyMany(set,chosen,fallbackSize);return set;
    }
    public static void RemoveMany(IconLibrary set,IEnumerable<HistoryEntry> entries){
        var paths=new HashSet<string>(entries.Select(e=>e.ImagePath),StringComparer.OrdinalIgnoreCase);
        var updated=new IconLibrary{Id=set.Id,Name=set.Name,Version=set.Version,Icons=set.Icons.Where(i=>!paths.Contains(ImagePath(set,i))).ToList()};
        Save(updated);set.Icons=updated.Icons; // Keep originals, as with single removal.
    }
    public static void Remove(IconLibrary set,string imagePath){var icon=set.Icons.FirstOrDefault(i=>ImagePath(set,i)==imagePath);if(icon==null)return;int index=set.Icons.IndexOf(icon);set.Icons.Remove(icon);try{Save(set);}catch{set.Icons.Insert(index,icon);throw;} // Keep unreferenced originals; removing from a set never changes the source gallery.
    }
    static string IcoEntry(LibraryIcon icon){string name=String.Concat(icon.Name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c)).Trim().TrimEnd('.');if(name.Length>60)name=name.Substring(0,60);if(name.Length==0)name="Icon";return "ico/"+name+"-"+icon.Id.Substring(0,8)+".ico";}
    static byte[] ReadLimited(ZipArchiveEntry entry,int limit){using(var input=entry.Open())using(var output=new MemoryStream()){var buffer=new byte[8192];int count;while((count=input.Read(buffer,0,buffer.Length))>0){if(output.Length+count>limit)throw new IOException("Icon-set entry exceeds size limit");output.Write(buffer,0,count);}return output.ToArray();}}
    public static void Export(IconLibrary set,string output){Validate(set);string full=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(full));string temp=Path.Combine(Path.GetDirectoryName(full),Guid.NewGuid().ToString("N")+".tmp");try{using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){var manifest=zip.CreateEntry("library.json");using(var writer=new StreamWriter(manifest.Open(),new UTF8Encoding(false)))writer.Write(Json().Serialize(set));foreach(var icon in set.Icons){string png=ImagePath(set,icon);if(!File.Exists(png))throw new IOException("Missing icon: "+icon.Name);zip.CreateEntryFromFile(png,"icons/"+icon.Id+".png");zip.CreateEntryFromFile(Path.ChangeExtension(png,".ico"),IcoEntry(icon));}}if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}}
    public static IconLibrary Import(string archive){Directory.CreateDirectory(Root);string staging=Path.Combine(Root,".import-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);try{using(var file=File.OpenRead(archive))using(var zip=new ZipArchive(file,ZipArchiveMode.Read)){if(zip.Entries.Count>601||zip.Entries.Sum(e=>e.Length)>512L*1024*1024)throw new IOException("Icon set is too large");var byName=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);foreach(var entry in zip.Entries){if(entry.FullName.Contains("..")||entry.FullName.Contains("\\")||entry.FullName.StartsWith("/")||entry.FullName.Contains(":")||byName.ContainsKey(entry.FullName))throw new IOException("Unsafe icon-set entry");byName.Add(entry.FullName,entry);}ZipArchiveEntry manifest;if(!byName.TryGetValue("library.json",out manifest)||manifest.Length>1024*1024)throw new IOException("Missing or oversized library.json");IconLibrary set;set=Json().Deserialize<IconLibrary>(Encoding.UTF8.GetString(ReadLimited(manifest,1024*1024)));Validate(set);var expected=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"library.json"};foreach(var icon in set.Icons){expected.Add("icons/"+icon.Id+".png");expected.Add(IcoEntry(icon));}if(!expected.SetEquals(byName.Keys))throw new IOException("Incomplete or unexpected icon-set files");Directory.CreateDirectory(Path.Combine(staging,"icons"));foreach(var icon in set.Icons){var entry=byName["icons/"+icon.Id+".png"];if(entry.Length>32L*1024*1024)throw new IOException("Icon image too large");using(var memory=new MemoryStream(ReadLimited(entry,32*1024*1024))){using(var image=Image.FromStream(memory)){if(image.Width>8192||image.Height>8192)throw new IOException("Icon dimensions too large");string png=Path.Combine(staging,"icons",icon.Id+".png");image.Save(png,ImageFormat.Png);Ico.Write(image,Path.ChangeExtension(png,".ico"),Ico.Sizes.Where(s=>s<=icon.MaxSize).ToArray());}}}set.Id=Guid.NewGuid().ToString("N");var names=new HashSet<string>(Read().Select(s=>s.Name),StringComparer.OrdinalIgnoreCase);string originalName=set.Name;int duplicate=1;while(names.Contains(set.Name)){string suffix=" (Import "+duplicate+++")";set.Name=originalName.Substring(0,Math.Min(originalName.Length,100-suffix.Length))+suffix;}File.WriteAllText(Path.Combine(staging,"library.json"),Json().Serialize(set),new UTF8Encoding(false));Directory.Move(staging,Folder(set));return set;}}finally{if(Directory.Exists(staging))Directory.Delete(staging,true);}}
}

class NameDialog : StudioWindow {
    readonly TextBox input;public string Value{get{return input.Text.Trim();}}
    public NameDialog(string title,string value){Text=title;Font=new Font(Theme.UiFont,10);BackColor=Theme.Surface;ForeColor=Theme.Text;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(440,135);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=MaximizeBox=false;var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),RowCount=2,ColumnCount=1};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));input=new TextBox{Text=value??"",MaxLength=100,Dock=DockStyle.Top,BackColor=Theme.Background,ForeColor=Theme.Text,Margin=new Padding(0,0,0,16)};root.Controls.Add(input,0,0);var row=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));var cancel=new DarkButton{Text=L.T("Abbrechen"),Dock=DockStyle.Top,Height=34,DialogResult=DialogResult.Cancel};var ok=new DarkButton{Text="OK",Primary=true,Dock=DockStyle.Top,Height=34};ok.Click+=(s,e)=>{if(Value.Length>0){DialogResult=DialogResult.OK;Close();}};row.Controls.Add(cancel);row.Controls.Add(ok);root.Controls.Add(row,0,1);Controls.Add(root);CancelButton=cancel;AcceptButton=ok;Shown+=(s,e)=>{Theme.TitleBar(this);input.SelectAll();input.Focus();};}
}
class LibraryBrowser : StudioWindow {
    List<IconLibrary> sets;IconLibrary active;Panel selectorHost,content;Gallery gallery;Button export,rename,remove,addCurrent,addFile,open,reference,copy;Label selectedLabel;readonly Image current;readonly string currentPrompt;readonly int maxSize;HistoryEntry selected;
    ContextMenuStrip copyMenu;Button newSet,selectAll,clearSelection;
    public HistoryEntry Selected;public bool AsReference;
    static string T(string de,string en){return LibraryStore.T(de,en);}
    public LibraryBrowser(Image artwork,string prompt,int size){
        current=artwork;currentPrompt=prompt;maxSize=size;SuspendLayout();Text=T("Meine Icons & Libraries","My icons & libraries");BackColor=Theme.Background;ForeColor=Theme.Text;Font=new Font(Theme.UiFont,10);AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1100,750);MinimumSize=new Size(800,560);StartPosition=FormStartPosition.CenterParent;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=5};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<5;i++)root.RowStyles.Add(new RowStyle(i==2?SizeType.Percent:SizeType.AutoSize,i==2?100:0));
        var bar=Row(5);selectorHost=new Panel{Dock=DockStyle.Top,Height=38,Margin=new Padding(0,0,8,6)};bar.Controls.Add(selectorHost,0,0);newSet=Btn(T("＋ Neue Library","＋ New library"),NewSet);bar.Controls.Add(newSet,1,0);rename=Btn(T("Umbenennen","Rename"),Rename);bar.Controls.Add(rename,2,0);bar.Controls.Add(Btn(T("Set importieren …","Import set …"),Import),3,0);export=Btn(T("Set exportieren …","Export set …"),Export);bar.Controls.Add(export,4,0);root.Controls.Add(bar,0,0);
        var tools=Row(4);addCurrent=Btn(T("Vorschau hinzufügen","Add preview"),AddCurrent);addFile=Btn(T("Icons hochladen …","Upload icons …"),AddFiles);copy=Btn(T("In Library kopieren …","Copy to library …"),CopySelected);remove=Btn(T("Aus Library entfernen","Remove from library"),Remove);tools.Controls.Add(addCurrent);tools.Controls.Add(addFile);tools.Controls.Add(copy);tools.Controls.Add(remove);root.Controls.Add(tools,0,1);
        content=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(content,0,2);
        selectedLabel=new Label{UseMnemonic=false,Text=T("Icon auswählen. Neue Libraries können eigene Sets sammeln.","Select an icon. Create a library to build your own set."),ForeColor=Theme.Muted,AutoSize=true,Margin=new Padding(0,10,0,10)};var selectionRow=new TableLayoutPanel{Dock=DockStyle.Top,Height=44,AutoSize=false,ColumnCount=3,RowCount=1,Margin=Padding.Empty};selectionRow.RowStyles.Add(new RowStyle(SizeType.Percent,100));selectionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));selectionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));selectionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));selectedLabel.AutoSize=false;selectedLabel.AutoEllipsis=true;selectedLabel.Dock=DockStyle.Fill;selectedLabel.TextAlign=ContentAlignment.MiddleLeft;selectedLabel.Margin=new Padding(0,0,10,6);selectionRow.Controls.Add(selectedLabel,0,0);selectAll=Btn(T("Alle auswählen","Select all"),()=>gallery.SelectAll(true));clearSelection=Btn(T("Auswahl aufheben","Clear selection"),()=>gallery.SelectAll(false));selectionRow.Controls.Add(selectAll,1,0);selectionRow.Controls.Add(clearSelection,2,0);root.Controls.Add(selectionRow,0,3);
        var actions=Row(3);actions.Controls.Add(Btn(L.T("Schließen"),()=>Close()));open=Btn(T("Icon öffnen","Open icon"),()=>Finish(false));reference=Btn(T("Als KI-Referenz verwenden","Use as AI reference"),()=>Finish(true));((DarkButton)reference).Primary=true;actions.Controls.Add(open);actions.Controls.Add(reference);root.Controls.Add(actions,0,4);Controls.Add(root);Reload(null);Shown+=(s,e)=>{Theme.TitleBar(this);var area=Screen.FromControl(this).WorkingArea;if(Width>area.Width||Height>area.Height){Size=new Size(Math.Min(Width,area.Width),Math.Min(Height,area.Height));Location=area.Location;}};FormClosed+=(s,e)=>{if(copyMenu!=null)copyMenu.Dispose();if(gallery!=null){gallery.Close();gallery.Dispose();}};ResumeLayout(true);
    }
    static TableLayoutPanel Row(int count){var row=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=count,RowCount=1,Margin=Padding.Empty};for(int i=0;i<count;i++)row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/count));return row;}
    Button Btn(string text,Action action){var b=new DarkButton{Text=text,Dock=DockStyle.Top,Height=38,Font=new Font(Theme.UiFont,9),Margin=new Padding(0,0,8,6)};b.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}};return b;}
    void Reload(string id){sets=LibraryStore.Read();active=sets.FirstOrDefault(s=>s.Id==id);foreach(Control c in selectorHost.Controls.Cast<Control>().ToArray()){selectorHost.Controls.Remove(c);c.Dispose();}var labels=new[]{T("Alle Generierungen","All generations")}.Concat(sets.Select(s=>s.Name)).ToArray();var selector=new StyleChoice(labels){Dock=DockStyle.Fill};selector.SelectedIndex=active==null?0:sets.IndexOf(active)+1;selector.Changed+=i=>{active=i==0?null:sets[i-1];ShowEntries();};selectorHost.Controls.Add(selector);ShowEntries();}
    void ShowEntries(){
        if(gallery!=null){gallery.Close();gallery.Dispose();}selected=null;
        var entries=active==null?History.Read():LibraryStore.Entries(active);
        gallery=new Gallery(entries){MultiSelect=true,TopLevel=false,FormBorderStyle=FormBorderStyle.None,Dock=DockStyle.Fill,MinimumSize=Size.Empty};
        gallery.SelectionChanged+=UpdateButtons;content.Controls.Clear();content.Controls.Add(gallery);
        gallery.SetCollectionCaption((active==null?T("Alle Generierungen","All generations"):active.Name)+" / "+entries.Count);gallery.Show();UpdateButtons();
    }
    void UpdateButtons(){
        var chosen=gallery==null?new List<HistoryEntry>():gallery.SelectedEntries;int count=chosen.Count;selected=count==1?chosen[0]:null;
        export.Enabled=rename.Enabled=addFile.Enabled=active!=null;addCurrent.Enabled=active!=null&&current!=null;
        open.Enabled=reference.Enabled=count==1;copy.Enabled=count>0;remove.Enabled=active!=null&&count>0;
        clearSelection.Enabled=count>0;selectAll.Enabled=gallery!=null;newSet.Text=T("＋ Neue Library","＋ New library")+(count>0?" ("+count+")":"");
        copy.Text=T("In Library kopieren","Copy to library")+(count>0?" ("+count+")":"")+" …";
        selectedLabel.Text=count==0?T("Keine Icons ausgewählt.","No icons selected."):count+T(" ausgewählt"," selected")+(count==1?" · "+LibraryStore.EntryName(selected):T(" · Gemeinsam kopieren oder entfernen."," · Copy or remove together."));
    }
    void NewSet(){CreateSetFrom(gallery.SelectedEntries);}
    void CreateSetFrom(List<HistoryEntry> chosen){
        string title=chosen.Count==0?T("Name der Library","Library name"):T("Neue Library für ","New library for ")+chosen.Count+T(" Icons"," icons");
        using(var dialog=new NameDialog(title,""))if(dialog.ShowDialog(this)==DialogResult.OK){var created=LibraryStore.CreateWithIcons(dialog.Value,chosen,maxSize);Reload(created.Id);selectedLabel.Text=chosen.Count+T(" Icons hinzugefügt."," icons added.");}
    }
    void Rename(){if(active==null)return;using(var dialog=new NameDialog(T("Library umbenennen","Rename library"),active.Name))if(dialog.ShowDialog(this)==DialogResult.OK){active.Name=dialog.Value;LibraryStore.Save(active);Reload(active.Id);}}
    void AddCurrent(){if(active==null||current==null)return;using(var dialog=new NameDialog(T("Name des Icons","Icon name"),T("Mein Icon","My icon")))if(dialog.ShowDialog(this)==DialogResult.OK){LibraryStore.Add(active,current,dialog.Value,currentPrompt,maxSize);Reload(active.Id);}}
    void AddFiles(){if(active==null)return;using(var d=new OpenFileDialog{Filter="Icons / Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.ico",Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK){int added=0;try{foreach(string file in d.FileNames){using(var image=Image.FromFile(file))LibraryStore.Add(active,image,Path.GetFileNameWithoutExtension(file).Substring(0,Math.Min(100,Path.GetFileNameWithoutExtension(file).Length)),"",maxSize);added++;}}catch(Exception ex){throw new IOException(added+T(" Icons hinzugefügt. Danach Fehler: "," icons added. Then failed: ")+ex.Message);}finally{Reload(active.Id);}}}
    void CopySelected(){
        var chosen=gallery.SelectedEntries;if(chosen.Count==0)return;
        if(copyMenu!=null){if(copyMenu.Visible){copyMenu.Close();return;}copyMenu.Dispose();}
        copyMenu=new ContextMenuStrip{BackColor=Theme.Surface,ForeColor=Theme.Text,ShowImageMargin=false,Renderer=new ToolStripProfessionalRenderer(new SizeDropdown.MenuColors())};
        var create=new ToolStripMenuItem(T("＋ Neue Library mit Auswahl …","＋ New library from selection …"));
        create.Click+=(s,e)=>{try{CreateSetFrom(chosen);}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}};copyMenu.Items.Add(create);
        if(sets.Count>0)copyMenu.Items.Add(new ToolStripSeparator());
        foreach(var set in sets){var target=set;var item=new ToolStripMenuItem(set.Name.Replace("&","&&"));item.Click+=(s,e)=>{try{CopyInto(target,chosen);}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}};copyMenu.Items.Add(item);}
        // Keep the menu alive until the next open / owner close, including while
        // a modal NameDialog closes it and Windows finishes dispatching its click.
        copyMenu.Show(copy,new Point(0,copy.Height));
    }
    void CopyInto(IconLibrary target,List<HistoryEntry> chosen){LibraryStore.CopyMany(target,chosen,maxSize);Reload(target.Id);selectedLabel.Text=chosen.Count+T(" Icons kopiert."," icons copied.");}
    void Remove(){
        var chosen=gallery.SelectedEntries;if(active==null||chosen.Count==0)return;
        if(MessageBox.Show(this,chosen.Count+T(" Icons aus dieser Library entfernen? Die ursprünglichen Generierungen bleiben erhalten."," icons: remove from this library? Original generations are preserved."),Text,MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        LibraryStore.RemoveMany(active,chosen);Reload(active.Id);
    }
    void Export(){if(active==null)return;using(var d=new SaveFileDialog{Filter="Icon set (*.iconset.zip)|*.iconset.zip",FileName=String.Concat(active.Name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c))+".iconset.zip",OverwritePrompt=true})if(d.ShowDialog(this)==DialogResult.OK){LibraryStore.Export(active,d.FileName);selectedLabel.Text=T("Set exportiert: ","Set exported: ")+d.FileName;}}
    void Import(){using(var d=new OpenFileDialog{Filter="Icon set (*.zip)|*.zip"})if(d.ShowDialog(this)==DialogResult.OK)Reload(LibraryStore.Import(d.FileName).Id);}
    void Finish(bool asReference){if(selected==null)return;Selected=selected;AsReference=asReference;DialogResult=DialogResult.OK;Close();}
}
}

namespace LocalIconStudio {
static class LibraryTests {
    public static void Run(string output){
        string oldRoot=LibraryStore.Root;LibraryStore.Root=Path.Combine(output,"libraries-"+Guid.NewGuid().ToString("N"));
        try{
            var set=LibraryStore.Create("TGC · Weiß & Türkis");
            using(var img=Image.FromFile(Path.Combine(output,"fixture.png"))){LibraryStore.Add(set,img,"TGC 5.8","Tausche nur 5.8 gegen 5.9 – weiß",64);LibraryStore.Add(set,img,"TGC 5.9","Andere Zahl",256);}
            string archive=Path.Combine(output,"roundtrip.iconset.zip");LibraryStore.Export(set,archive);var imported=LibraryStore.Import(archive);
            if(imported.Id==set.Id||imported.Name==set.Name||imported.Icons.Count!=2||imported.Icons[0].Prompt!=set.Icons[0].Prompt||imported.Icons[0].MaxSize!=64)throw new Exception("Library roundtrip/duplicate import failed");
            if(!File.ReadAllBytes(LibraryStore.ImagePath(set,set.Icons[0])).SequenceEqual(File.ReadAllBytes(LibraryStore.ImagePath(imported,imported.Icons[0]))))throw new Exception("Library PNG changed on import");
            using(var r=new BinaryReader(File.OpenRead(Path.ChangeExtension(LibraryStore.ImagePath(imported,imported.Icons[0]),".ico")))){r.BaseStream.Position=4;if(r.ReadUInt16()!=5)throw new Exception("Library export size not preserved");}
            string hostile=Path.Combine(output,"hostile.iconset.zip");using(var file=File.Create(hostile))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){using(var w=new StreamWriter(zip.CreateEntry("../escaped.txt").Open()))w.Write("invalid");}
            int count=LibraryStore.Read().Count;bool rejected=false;try{LibraryStore.Import(hostile);}catch(IOException){rejected=true;}if(!rejected||LibraryStore.Read().Count!=count||File.Exists(Path.Combine(LibraryStore.Root,"escaped.txt")))throw new Exception("Unsafe archive modified library store");
            string broken=Path.Combine(output,"broken.iconset.zip");using(var file=File.Create(broken))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){zip.CreateEntryFromFile(Path.Combine(LibraryStore.Folder(set),"library.json"),"library.json");}
            rejected=false;try{LibraryStore.Import(broken);}catch(IOException){rejected=true;}if(!rejected||LibraryStore.Read().Count!=count)throw new Exception("Incomplete archive changed existing sets");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            using(var browser=new LibraryBrowser(null,"",256)){browser.Show();Application.DoEvents();typeof(LibraryBrowser).GetMethod("Reload",flags).Invoke(browser,new object[]{imported.Id});Application.DoEvents();Tests.Capture(browser,Path.Combine(output,"libraries.png"));var gallery=(Gallery)typeof(LibraryBrowser).GetField("gallery",flags).GetValue(browser);var grid=(FlowLayoutPanel)typeof(Gallery).GetField("grid",flags).GetValue(gallery);typeof(Control).GetMethod("OnClick",flags).Invoke(grid.Controls[0],new object[]{EventArgs.Empty});if(!browser.Visible)throw new Exception("Icon selection unexpectedly closed browser");typeof(LibraryBrowser).GetMethod("Finish",flags).Invoke(browser,new object[]{true});if(browser.Selected==null||!browser.AsReference)throw new Exception("Library reference selection failed");}
            using(var form=new Studio(null)){form.Show();form.LoadArtwork(Path.Combine(output,"fixture.png"));form.SetPrompt("Ändere nur den Text auf 5.9");form.SetReference(Path.Combine(output,"fixture.png"));if(!form.HasReference||form.CurrentPrompt!="Ändere nur den Text auf 5.9")throw new Exception("Reference selection changed prompt");Application.DoEvents();Tests.Capture(form,Path.Combine(output,"reference-preview.png"));form.ClearReference();if(form.HasReference||form.CurrentPrompt!="Ändere nur den Text auf 5.9")throw new Exception("Reference removal changed state");form.Close();}
            if(Codex.ReferenceArgument(@"C:\Images\a b.png")!=" --image \"C:\\Images\\a b.png\""||Codex.ReferenceArgument(null)!="")throw new Exception("Reference CLI quoting failed");
            LibraryStore.Remove(imported,LibraryStore.ImagePath(imported,imported.Icons[0]));if(LibraryStore.Read().First(s=>s.Id==imported.Id).Icons.Count!=1||LibraryStore.Read().First(s=>s.Id==set.Id).Icons.Count!=2)throw new Exception("Removing from imported library changed original");
            L.Set(true,false);using(var browser=new LibraryBrowser(null,"",256)){browser.Show();Application.DoEvents();typeof(LibraryBrowser).GetMethod("Reload",flags).Invoke(browser,new object[]{set.Id});Application.DoEvents();Tests.Capture(browser,Path.Combine(output,"libraries-english.png"));browser.Close();}L.Set(false,false);
            LibraryBatchTests.Run(output);
            File.WriteAllText(Path.Combine(output,"LIBRARIES-PASS.txt"),"PASS: create, PNG/ICO/prompt/max-size roundtrip, non-overwriting import, hostile/incomplete archive rejection, selection as reference, state preservation, reference CLI attachment quoting; batch UI, modal copy/new-library/cancel, export/import, rename/remove and atomic failure checks. No online edit invoked.");
        }finally{LibraryStore.Root=oldRoot;}
    }
}
static class LibraryBatchTests {
    const System.Reflection.BindingFlags Flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    static object Field(object obj,string name){return obj.GetType().GetField(name,Flags).GetValue(obj);}
    static object Call(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,Flags).Invoke(obj,args);}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void ClickNameDialog(ToolStripItem item,bool accept,string name){
        bool answered=false;using(var timer=new Timer{Interval=30}){
            timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.OfType<NameDialog>().FirstOrDefault();if(dialog==null)return;timer.Stop();answered=true;((TextBox)Field(dialog,"input")).Text=name;if(accept)dialog.AcceptButton.PerformClick();else dialog.CancelButton.PerformClick();};
            timer.Start();item.PerformClick();timer.Stop();Application.DoEvents();
        }
        Check(answered,"Library name dialog was not exercised");
    }
    public static void Run(string output){
        var source=LibraryStore.Create("Batch source");
        using(var image=Image.FromFile(Path.Combine(output,"fixture.png"))){
            LibraryStore.Add(source,image,"First · Weiß","Keep first prompt",64);
            LibraryStore.Add(source,image,"Second","Keep second prompt",256);
            LibraryStore.Add(source,image,"Third","Keep third prompt",128);
        }
        var original=LibraryStore.Entries(source);var originalBytes=File.ReadAllBytes(original[0].ImagePath);
        IconLibrary created;
        using(var browser=new LibraryBrowser(null,"",256)){
            browser.Show();Call(browser,"Reload",source.Id);Application.DoEvents();
            var gallery=(Gallery)Field(browser,"gallery");var grid=(FlowLayoutPanel)Field(gallery,"grid");
            Check(!((Button)Field(browser,"copy")).Enabled,"Empty selection can copy");
            typeof(Control).GetMethod("OnClick",Flags).Invoke(grid.Controls[0],new object[]{EventArgs.Empty});
            Check(gallery.SelectedEntries.Count==1&&((Button)Field(browser,"open")).Enabled,"Single selection cannot open");
            typeof(Control).GetMethod("OnClick",Flags).Invoke(grid.Controls[1],new object[]{EventArgs.Empty});
            Check(gallery.SelectedEntries.Count==2&&!((Button)Field(browser,"open")).Enabled&&!((Button)Field(browser,"reference")).Enabled,"Multiple selection opens ambiguous icon");
            Check(((Button)Field(browser,"copy")).Text.Contains("2")&&((Label)Field(browser,"selectedLabel")).Text.Contains("2"),"Selection count missing");
            Tests.Capture(browser,Path.Combine(output,"libraries-multi-de.png"));
            int setCount=LibraryStore.Read().Count;
            ((Button)Field(browser,"copy")).PerformClick();var menu=(ContextMenuStrip)Field(browser,"copyMenu");
            ClickNameDialog(menu.Items[0],false,"Cancelled library");
            Check(!menu.IsDisposed&&gallery.SelectedEntries.Count==2&&LibraryStore.Read().Count==setCount,"Cancelled new-library action loses selection or disposes menu");
            ((Button)Field(browser,"copy")).PerformClick();Check(menu.IsDisposed,"Old menu not cleaned up");menu=(ContextMenuStrip)Field(browser,"copyMenu");
            ClickNameDialog(menu.Items[0],true,"Batch · Neue Auswahl");
            Check(!menu.IsDisposed,"Modal batch-copy menu disposed mid-click");
            created=LibraryStore.Read().Single(s=>s.Name=="Batch · Neue Auswahl");
            Check(created.Icons.Count==2&&created.Icons[0].Name==source.Icons[0].Name&&created.Icons[0].Prompt==source.Icons[0].Prompt&&created.Icons[0].MaxSize==64,"Batch copy lost metadata");
            Check(LibraryStore.Read().Single(s=>s.Id==source.Id).Icons.Count==3,"New library changed source");
            gallery=(Gallery)Field(browser,"gallery");
            ((Button)Field(browser,"selectAll")).PerformClick();Check(gallery.SelectedEntries.Count==2,"Select-all failed");
            ((Button)Field(browser,"clearSelection")).PerformClick();Check(gallery.SelectedEntries.Count==0,"Clear selection failed");
            gallery.SelectAll(true);((Button)Field(browser,"copy")).PerformClick();menu=(ContextMenuStrip)Field(browser,"copyMenu");
            menu.Items.Cast<ToolStripItem>().Single(i=>i.Text==source.Name).PerformClick();Application.DoEvents();
            Check(!menu.IsDisposed&&LibraryStore.Read().Single(s=>s.Id==source.Id).Icons.Count==5,"Copy into existing library failed");
            Check(originalBytes.SequenceEqual(File.ReadAllBytes(original[0].ImagePath)),"Batch copy changed original image");
            browser.Close();Check(menu.IsDisposed,"Library owner leaves menu alive");
        }
        string manifest=Path.Combine(LibraryStore.Folder(created),"library.json");byte[] before=File.ReadAllBytes(manifest);
        string[] filesBefore=Directory.GetFiles(Path.Combine(LibraryStore.Folder(created),"icons")).OrderBy(s=>s).ToArray();
        bool failed=false;try{LibraryStore.CopyMany(created,new[]{original[0],new HistoryEntry{ImagePath=Path.Combine(output,"missing-batch.png"),Name="Missing",Prompt="",MaxSize=256}},256);}catch(FileNotFoundException){failed=true;}
        Check(failed&&created.Icons.Count==2&&before.SequenceEqual(File.ReadAllBytes(manifest)),"Failed batch changed manifest or in-memory selection");
        Check(filesBefore.SequenceEqual(Directory.GetFiles(Path.Combine(LibraryStore.Folder(created),"icons")).OrderBy(s=>s)),"Failed batch left partial files");
        failed=false;try{LibraryStore.CopyMany(created,Enumerable.Repeat(original[0],301),256);}catch(IOException){failed=true;}
        Check(failed&&before.SequenceEqual(File.ReadAllBytes(manifest)),"Batch capacity check changes library");
        int countBefore=LibraryStore.Read().Count;failed=false;
        try{LibraryStore.CreateWithIcons("Must not appear",new[]{original[0],new HistoryEntry{ImagePath=Path.Combine(output,"absent.png"),Name="Missing",MaxSize=256}},256);}catch(FileNotFoundException){failed=true;}
        Check(failed&&LibraryStore.Read().Count==countBefore,"Failed batch published an incomplete library");
        created.Name="Renamed batch";LibraryStore.Save(created);Check(LibraryStore.Read().Single(s=>s.Id==created.Id).Name==created.Name,"Rename failed");
        string archive=Path.Combine(output,"batch.iconset.zip");LibraryStore.Export(created,archive);var imported=LibraryStore.Import(archive);
        Check(imported.Id!=created.Id&&imported.Icons.Count==2&&imported.Icons[0].MaxSize==64&&imported.Icons[1].Prompt==created.Icons[1].Prompt,"Batch archive roundtrip failed");
        LibraryStore.RemoveMany(imported,LibraryStore.Entries(imported));Check(LibraryStore.Read().Single(s=>s.Id==imported.Id).Icons.Count==0&&LibraryStore.Read().Single(s=>s.Id==created.Id).Icons.Count==2,"Batch remove changes original library");
        L.Set(true,false);using(var browser=new LibraryBrowser(null,"",256)){browser.Show();Call(browser,"Reload",created.Id);((Gallery)Field(browser,"gallery")).SelectAll(true);Application.DoEvents();Tests.Capture(browser,Path.Combine(output,"libraries-multi-en.png"));browser.Close();}L.Set(false,false);
        File.WriteAllText(Path.Combine(output,"LIBRARY-BATCH-PASS.txt"),"PASS: two-tile selection, counts and action availability; real modal new-library accept/cancel; existing-library menu dispatch; selection preservation; all/clear; prompt/name/size metadata; original PNG preservation; atomic rollback and capacity; rename; batch export/import; batch removal; menu cleanup; German/English renders.");
    }
}
}
