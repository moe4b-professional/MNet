"use strict";
var NativeWebSocketLibrary = /** @class */ (function () {
    function NativeWebSocketLibrary() {
    }
    NativeWebSocketLibrary.WS_Connect = function (p_url) {
        var url = Pointer_stringify(p_url);
        var id = NativeWebSocketCollection.Assign();
        var socket = new WebSocket(url);
        NativeWebSocketCollection.Add(id, socket);
        return id;
    };
    NativeWebSocketLibrary.WS_CheckState = function (id) {
        var socket = NativeWebSocketCollection.dictionary[id];
        if (socket == null)
            return -1;
        return socket.readyState;
    };
    NativeWebSocketLibrary.WS_Send = function (id, buffer) {
        var socket = NativeWebSocketCollection.dictionary[id];
        if (socket == null)
            return -1;
        if (socket.readyState != WebSocket.OPEN)
            return -2;
        socket.send(buffer);
        return 0;
    };
    NativeWebSocketLibrary.WS_Disconnect = function (id, code, p_reason) {
        var socket = NativeWebSocketCollection.Remove(id);
        if (socket == null)
            return -1;
        var reason = Pointer_stringify(p_reason);
        socket.close(code, reason);
        return 0;
    };
    NativeWebSocketLibrary.$NativeWebSocketCollection = {
        //#region Class
        dictionary: {},
        vacant: [],
        index: 0,
        Assign: function () {
            if (this.vacant.length == 0) {
                var value = this.index;
                this.index += 1;
                return value;
            }
            else {
                var value = this.vacant[0];
                this.vacant.slice(0, 1);
                return value;
            }
        },
        Add: function (id, socket) {
            this.dictionary[id] = socket;
            socket.onopen = function (event) {
                console.log("Opened");
            };
            socket.onmessage = function (event) {
            };
            return true;
        },
        Remove: function (id) {
            var socket = this.dictionary[id];
            if (socket == null)
                return null;
            delete this.dictionary[id];
            return socket;
        }
        //#endregion
    };
    return NativeWebSocketLibrary;
}());
autoAddDeps(NativeWebSocketLibrary, "$NativeWebSocketCollection");
mergeInto(LibraryManager.library, NativeWebSocketLibrary);
