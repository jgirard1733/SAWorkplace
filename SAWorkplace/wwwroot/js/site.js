
function addNotificaton() {
    document.getElementById('favicon').href = '/img/favicon-alert.png';
}
function removeNotificaton() {
    document.getElementById('favicon').href = '/img/favicon-16x16.png';
}

(async function () {
    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/signalServer')
        .build();

    connection.on('Review_Added', function (message) {
        addNotificaton();
    });
    await connection.start();
})();