class NativeWebSocketLibrary
{
    static $NativeWebSocketCollection: INativeWebSocketCollection = {
        //#region Class
        dictionary: {},
        vacant: [],
        index: 0,

        Assign: function (): number
        {
            if (this.vacant.length == 0)
            {
                let value = this.index;

                this.index += 1;

                return value;
            }
            else
            {
                let value = this.vacant[0];

                this.vacant.slice(0, 1);

                return value;
            }
        },

        Add: function (id: number, socket: WebSocket): boolean
        {
            this.dictionary[id] = socket;

            socket.onopen = (event: Event) =>
            {
                console.log("Opened");
            };

            socket.onmessage = (event: Event) => 
            {

            };

            return true;
        },

        Remove: function (id: number): WebSocket | null
        {
            let socket = this.dictionary[id];

            if (socket == null) return null;

            delete this.dictionary[id];

            return socket;
        }
        //#endregion
    }

    static WS_Connect(p_url: any): number
    {
        let url = Pointer_stringify(p_url);

        let id = NativeWebSocketCollection.Assign();
        let socket = new WebSocket(url);

        NativeWebSocketCollection.Add(id, socket);

        return id;
    }

    static WS_CheckState(id: number): number
    {
        let socket = NativeWebSocketCollection.dictionary[id];

        if (socket == null) return -1;

        return socket.readyState;
    }

    static WS_Send(id: number, buffer: ArrayBuffer): number
    {
        let socket = NativeWebSocketCollection.dictionary[id];

        if (socket == null) return -1;

        if (socket.readyState != WebSocket.OPEN) return -2;

        socket.send(buffer);

        return 0;
    }

    static WS_Disconnect(id: number, code: number, p_reason: any): number
    {
        let socket = NativeWebSocketCollection.Remove(id);

        if (socket == null) return -1;

        let reason = Pointer_stringify(p_reason);

        socket.close(code, reason);

        return 0;
    }
}

autoAddDeps(NativeWebSocketLibrary, "$NativeWebSocketCollection");
mergeInto(LibraryManager.library, NativeWebSocketLibrary);