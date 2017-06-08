// Write your Javascript code.

var protocol = location.protocol === "https:" ? "wss:" : "ws:";
var wsUri = protocol + "//" + window.location.host + window.location.pathname;
var socket = new WebSocket(wsUri);

socket.onmessage = function (data) {
    console.log(data);
    $("#responseText").text(data.data)
}

socket.onopen = function () {
    socket.send(JSON.stringify({
        id: "client1"
    }));
}