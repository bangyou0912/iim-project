mergeInto(LibraryManager.library, {
    DownloadCSV: function(fileNamePtr, contentPtr) {
        var fileName = UTF8ToString(fileNamePtr);
        var content = UTF8ToString(contentPtr);
        var blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
});
