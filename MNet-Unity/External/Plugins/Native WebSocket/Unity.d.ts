declare function Pointer_stringify(pointer: any): string;

declare function mergeInto(target: any, source: any): void;
declare function autoAddDeps(a: any, b: string): void;

declare var LibraryManager: any;
declare var LibraryMyPlugin: any;

declare interface INativeWebSocketCollection
{
    dictionary: { [id: number]: WebSocket; };

    vacant: Array<number>;

    index: number;

    Assign(): number;

    Add(id: number, socket: WebSocket): boolean;

    Remove(id: number): WebSocket | null;
}

declare var NativeWebSocketCollection: INativeWebSocketCollection;