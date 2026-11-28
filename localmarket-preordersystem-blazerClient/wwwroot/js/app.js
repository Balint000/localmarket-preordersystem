// Fájl letöltése bájtokból (export végpontok). Csak a böngészőoldali mentést végzi.
window.lmDownloadFile = (fileName, contentType, base64) => {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};

// Világos/sötét téma. A kezdeti értéket az App.razor <head> szkriptje állítja be festés előtt.
window.lmTheme = {
    get: () => document.documentElement.getAttribute('data-theme') || 'light',
    set: (theme) => {
        const t = theme === 'dark' ? 'dark' : 'light';
        const d = document.documentElement;
        d.setAttribute('data-theme', t);
        d.setAttribute('data-bs-theme', t);
        try { localStorage.setItem('lm.theme', t); } catch (e) { /* privát mód: csak erre a munkamenetre */ }
        return t;
    }
};

// Mobil oldalsáv: nyitáskor az első linkre, záráskor vissza a menügombra kerül a fókusz.
window.lmFocus = (selector) => {
    const el = document.querySelector(selector);
    if (el) el.focus();
};
