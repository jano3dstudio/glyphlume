(() => {
  const language = document.querySelector('#language');
  const size = document.querySelector('#icon-size');
  const translated = [...document.querySelectorAll('[data-en]')];
  translated.forEach(el => { el.dataset.de = el.innerHTML; });
  const updateSize = () => {
    const maximum = Number(size.value);
    let count = 0;
    document.querySelectorAll('[data-size]').forEach(el => {
      const included = Number(el.dataset.size) <= maximum;
      el.classList.toggle('excluded', !included);
      if (included) count++;
    });
    document.querySelector('#size-preview').style.width = `${Math.min(210, maximum)}px`;
    document.querySelector('#size-description').textContent = language.value === 'en'
      ? `${count} ${count === 1 ? 'size' : 'sizes'}. One .ico file.`
      : `${count} ${count === 1 ? 'Größe' : 'Größen'}. Eine .ico-Datei.`;
  };
  const applyLanguage = () => {
    const lang = language.value;
    document.documentElement.lang = lang;
    translated.forEach(el => { el.innerHTML = el.dataset[lang]; });
    document.title = lang === 'en' ? 'GLYPHLUME — Your idea. Your icon.' : 'GLYPHLUME — Deine Idee. Dein Icon.';
    updateSize();
    try { localStorage.setItem('glyph-forge-language', lang); } catch (_) {}
  };
  try { const saved = localStorage.getItem('glyph-forge-language'); if (saved === 'en' || saved === 'de') language.value = saved; } catch (_) {}
  language.addEventListener('change', applyLanguage);
  size.addEventListener('change', updateSize);
  applyLanguage();
})();
