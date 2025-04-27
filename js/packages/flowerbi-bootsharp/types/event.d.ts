/** Allows broadcasting events. */
export interface EventBroadcaster<T extends unknown[]> {
    /** Notifies attached handlers with specified payload.
     *  @param args The payload of the notification. */
    broadcast: (...args: [...T]) => void;
}
/** Allows attaching event handlers. */
export interface EventSubscriber<T extends unknown[]> {
    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    subscribe: (handler: (...args: [...T]) => void) => string;
    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    unsubscribe: (handler: (...args: [...T]) => void) => void;
    /** In case event was broadcast at least once, returns last payload; undefined otherwise. */
    readonly last?: [...T];
}
/** Optional configuration of an event instance. */
export type EventOptions = {
    /** Custom warnings handler; by default <code>console.warn</code> is used. */
    warn?: (message: string) => void;
};
/** Allows attaching handlers and broadcasting events. */
export declare class Event<T extends unknown[]> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers;
    private readonly warn;
    private lastArgs?;
    /** Creates new event instance. */
    constructor(options?: EventOptions);
    /** Notifies attached handlers with specified payload.
     *  @param args The payload of the notification. */
    broadcast(...args: [...T]): void;
    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    subscribe(handler: (...args: [...T]) => void): string;
    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    unsubscribe(handler: (...args: [...T]) => void): void;
    /** Attaches handler with specified identifier for events emitted by this event instance.
     *  @param id Identifier of the handler.
     *  @param handler The handler to attach. */
    subscribeById(id: string, handler: (...args: [...T]) => void): void;
    /** Detaches handler with specified identifier from events emitted by this event instance.
     *  @param id Identifier of the handler. */
    unsubscribeById(id: string): void;
    /** In case event was broadcast at least once, returns last payload; undefined otherwise. */
    get last(): [...T] | undefined;
    private getOrDefineId;
}
