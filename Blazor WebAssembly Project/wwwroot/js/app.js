// wwwroot/js/app.js
window.blazorConsoleLog = {
    log: function (message) {
        console.log(message);
    },
    warn: function (message) {
        console.warn(message);
    },
    error: function (message) {
        console.error(message);
    },
    clear: function () {
        console.clear();
    }
};
