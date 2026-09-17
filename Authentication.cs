using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalIconStudio {
enum AuthState { Checking, SignedIn, Verified, Missing, Expired, ApiKey, Unavailable, Unknown }
class AuthenticationException : IOException { public AuthenticationException(string message):base(message){} }
static class Authentication {
    public static bool IsAuthenticationFailure(string message){
        string text=(message??"").ToLowerInvariant();
        return text.Contains("refresh_token_reused")||text.Contains("refresh_token_expired")||text.Contains("refresh_token_invalidated")||text.Contains("access token could not be refreshed")||text.Contains("token has expired")||text.Contains("invalid_grant")||text.Contains("not authenticated")||text.Contains("401 unauthorized")||text.Contains("please sign in again");
    }
    public static AuthState Parse(int code,string output){
        string text=(output??"").ToLowerInvariant();
        if(IsAuthenticationFailure(text))return AuthState.Expired;
        if(text.Contains("not logged in"))return AuthState.Missing;
        if(code==0&&text.Contains("logged in using chatgpt"))return AuthState.SignedIn;
        if(code==0&&(text.Contains("api key")||text.Contains("api_key")))return AuthState.ApiKey;
        return AuthState.Unknown;
    }
    public static async Task<AuthState> Check(){
        try{
            using(var p=Codex.Start("login status",Program.Root)){
                var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();p.StandardInput.Close();
                if(!await Task.Run(()=>p.WaitForExit(10000))){try{p.Kill();}catch{}return AuthState.Unknown;}
                // Only classify the result. Never display raw login output (API-key logins may contain key fragments).
                return Parse(p.ExitCode,(await output)+"\n"+(await error));
            }
        }catch(FileNotFoundException){return AuthState.Unavailable;}catch(System.ComponentModel.Win32Exception){return AuthState.Unavailable;}catch(IOException){return AuthState.Unavailable;}catch(UnauthorizedAccessException){return AuthState.Unknown;}
    }
    public static bool Ready(AuthState state){return state==AuthState.SignedIn||state==AuthState.Verified;}
    public static string Caption(AuthState state){
        switch(state){
            case AuthState.Checking:return L.T("ChatGPT · Prüfen …");
            case AuthState.SignedIn:return L.T("ChatGPT · Angemeldet");
            case AuthState.Verified:return L.T("ChatGPT · Verbunden");
            case AuthState.Missing:return L.T("ChatGPT · Nicht angemeldet");
            case AuthState.Expired:return L.T("ChatGPT · Login erneuern");
            case AuthState.ApiKey:return L.T("ChatGPT · Abo anmelden");
            case AuthState.Unavailable:return L.T("ChatGPT · Setup nötig");
            default:return L.T("ChatGPT · Status unklar");
        }
    }
    public static string Description(AuthState state){
        switch(state){
            case AuthState.SignedIn:return L.T("ChatGPT-Anmeldung vorhanden. Die Online-Verbindung wird beim Generieren geprüft.");
            case AuthState.Verified:return L.T("ChatGPT hat die letzte Bildgenerierung erfolgreich abgeschlossen.");
            case AuthState.Missing:return L.T("Du bist noch nicht mit ChatGPT angemeldet.");
            case AuthState.Expired:return L.T("Deine Anmeldung konnte nicht erneuert werden. Bitte melde dich erneut mit ChatGPT an.");
            case AuthState.ApiKey:return L.T("Es ist ein API-Zugang aktiv. Für dieses Tool bitte mit deinem ChatGPT-Abo anmelden.");
            case AuthState.Unavailable:return L.T("Codex CLI wurde nicht gefunden. Installiere zuerst Codex, um die Bildgenerierung zu nutzen.");
            default:return L.T("Der Anmeldestatus konnte nicht festgestellt werden. Du kannst es über den ChatGPT-Button erneut versuchen.");
        }
    }
}
class ConnectionDialog : StudioWindow {
    public ConnectionDialog(AuthState state){
        SuspendLayout();
        Text="GLYPHLUME · ChatGPT";BackColor=Theme.Background;ForeColor=Theme.Text;Font=new Font(Theme.UiFont,10);AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        ClientSize=new Size(620,400);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=4};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(layout);
        layout.Controls.Add(new Label{Text=L.T("DEIN ZUGANG ZUR BILDGENERIERUNG"),Font=new Font(Theme.UiFont,17),AutoSize=true,ForeColor=Theme.Accent,Margin=new Padding(0,0,0,16)},0,0);
        layout.Controls.Add(new Label{Text=Authentication.Description(state),AutoSize=true,MaximumSize=new Size(515,0),Margin=new Padding(0,0,0,16)},0,1);
        layout.Controls.Add(new Label{Text=L.T("Anbieter: ChatGPT / Codex\nMelde dich mit deinem bestehenden Abo im Browser an.\n\nOhne Login kannst du Bilder laden und als ICO speichern.\nWeitere Anbieter sind derzeit nicht angebunden."),AutoSize=true,MaximumSize=new Size(515,0),ForeColor=Theme.Muted,Margin=new Padding(0,0,0,20)},0,2);
        var row=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));layout.Controls.Add(row,0,3);
        var signIn=new DarkButton{Text=Authentication.Ready(state)?L.T("Erneut anmelden"):L.T("Mit ChatGPT anmelden"),Primary=true,Dock=DockStyle.Top,Height=44,DialogResult=DialogResult.OK,Enabled=state!=AuthState.Unavailable,Margin=new Padding(0,0,10,0)};
        var later=new DarkButton{Text=Authentication.Ready(state)?L.T("Weiter"):L.T("Nur ICO-Export"),Dock=DockStyle.Top,Height=44,DialogResult=DialogResult.Cancel,Margin=Padding.Empty};row.Controls.Add(signIn,0,0);row.Controls.Add(later,1,0);AcceptButton=signIn;CancelButton=later;
        Shown+=(s,e)=>Theme.TitleBar(this);
        ResumeLayout(true);
    }
}
}
