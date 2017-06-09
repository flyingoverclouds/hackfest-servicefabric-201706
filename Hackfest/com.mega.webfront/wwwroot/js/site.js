// Write your Javascript code.

var intervalId = null;
var openSocket = function () {
    var responseId = $("#tip").html().trim();
    if (responseId) {
        clearInterval(intervalId);

        var protocol = location.protocol === "https:" ? "wss:" : "ws:";
        var wsUri = protocol + "//" + window.location.host + window.location.pathname;
        var socket = new WebSocket(wsUri);

        socket.onmessage = function (data) {
            console.log(data);
            $("#responseText").text(data.data)
        }

        socket.onopen = function () {
            socket.send(responseId);
        }
    }
};

$(document).ready(function () {
    intervalId = setInterval(openSocket, 1000);
});