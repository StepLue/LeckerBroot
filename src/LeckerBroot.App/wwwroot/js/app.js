// Kleine Helfer, die C# über JS-Interop aufruft.
window.leckerBroot = {
    // Textdatei herunterladen (Sicherung). Auf dem iPad öffnet Safari den Teilen-/Sichern-Dialog.
    download: function (fileName, text, mime) {
        const blob = new Blob([text], { type: mime || "application/json" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    }
};
