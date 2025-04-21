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
    info: function (message) {
        console.info(message);
    },
    table: function (data) {
        console.table(data);
    },
    dir: function (object) {
        console.dir(object);
    },
    dirxml: function (object) {
        console.dirxml(object);
    },
    group: function (label) {
        console.group(label);
    },
    groupEnd: function () {
        console.groupEnd();
    },
    time: function (label) {
        console.time(label);
    },
    timeEnd: function (label) {
        console.timeEnd(label);
    },
    count: function (label) {
        console.count(label);
    },
    clear: function () {
        console.clear();
    }
};
// Add this function to wwwroot/js/app.js
window.getUserRole = function () {
    // Try to get the user role from the token in local storage
    const token = localStorage.getItem('authToken');

    if (!token) {
        return '';
    }

    try {
        // Parse the JWT token (it's in format header.payload.signature)
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const payload = JSON.parse(jsonPayload);

        // Look for role claim - different formats depending on token issuer
        const role = payload.role ||
            payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
            '';

        console.log("Role from token:", role);
        return role;
    } catch (e) {
        console.error("Error parsing JWT token:", e);
        return '';
    }
};
