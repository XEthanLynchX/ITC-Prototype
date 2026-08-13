import { ref, reactive, shallowRef } from 'vue';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

/**
 * Wraps the SignalR connection in reactive state. Everything the UI renders lives here;
 * the components are presentational. Not a Pinia store — one connection per tab and no
 * cross-route state to share, so a composable covers it.
 */
export function useWiuConnection() {
  const status = ref('disconnected');   // disconnected | connecting | connected | reconnecting
  const group = ref('(none)');
  const batchCount = ref(0);
  const connectionId = ref(null);

  // Keyed by WIU id. reactive() rather than ref({}) so patching one WIU does not
  // replace the whole object and re-render every row.
  const wiuState = reactive({});
  const log = ref([]);

  // Must not be deep-reactive: Vue would proxy SignalR's internals and break the transport.
  const connection = shallowRef(null);

  // What this tab *wants*, kept separate from `group` (where the server actually put
  // us) so a reconnect can re-declare it without reading the DOM.
  let desiredWius = [];

  function addLog(message) {
    log.value.unshift({
      at: new Date().toISOString().substring(11, 23),
      message,
    });
    if (log.value.length > 100) log.value.pop();
  }

  function replaceState(next) {
    for (const key of Object.keys(wiuState)) delete wiuState[key];
    Object.assign(wiuState, next);
  }

  async function start() {
    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/wiu')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Handlers must not return a value. An expression-bodied arrow like
    // (b) => Object.assign(wiuState, b) returns the object, which SignalR reports as the
    // result of an invocation the server never made:
    // "Result given for 'receivewiubatch' method but server is not expecting a result."
    conn.on('ReceiveWiuBatch', (batch) => {
      batchCount.value++;
      Object.assign(wiuState, batch);
      addLog(`batch: ${Object.keys(batch).sort().join(', ')}`);
    });

    // Snapshots replace local state rather than merging, so WIUs from a previous layout
    // do not linger after a subscription change.
    conn.on('ReceiveSnapshot', (snapshot) => {
      replaceState(snapshot);
      addLog(`snapshot: ${Object.keys(snapshot).sort().join(', ') || '(empty)'}`);
    });

    conn.on('SubscriptionChanged', (groupName) => {
      group.value = groupName;
      addLog(`group -> ${groupName}`);
    });

    conn.onreconnecting(() => {
      status.value = 'reconnecting';
      addLog('reconnecting...');
    });

    // Group membership does not survive a reconnect — the connection id is new, so the
    // client must re-declare what it wants or sit silent forever.
    conn.onreconnected(async () => {
      status.value = 'connected';
      connectionId.value = conn.connectionId;
      addLog('reconnected — resending subscription');
      await applySubscription(desiredWius);
    });

    conn.onclose(() => {
      status.value = 'disconnected';
      group.value = '(none)';
      addLog('connection closed');
    });

    connection.value = conn;
    status.value = 'connecting';

    try {
      await conn.start();
      status.value = 'connected';
      connectionId.value = conn.connectionId;
      addLog('connected');
    } catch (err) {
      status.value = 'disconnected';
      addLog(`start failed: ${err.message}`);
    }
  }

  async function applySubscription(wius) {
    desiredWius = [...wius];

    const conn = connection.value;
    if (!conn || conn.state !== HubConnectionState.Connected) return;

    try {
      if (desiredWius.length === 0) {
        await conn.invoke('Unsubscribe');
        replaceState({});
      } else {
        await conn.invoke('Subscribe', desiredWius);
      }
    } catch (err) {
      addLog(`error: ${err.message}`);
    }
  }

  /** Leave the group without disconnecting — what a hidden tab does. */
  async function leaveGroup() {
    const conn = connection.value;
    if (!conn || conn.state !== HubConnectionState.Connected) return;

    try {
      await conn.invoke('Unsubscribe');
      addLog('tab hidden -> left group');
    } catch (err) {
      addLog(`error: ${err.message}`);
    }
  }

  async function rejoinGroup() {
    await applySubscription(desiredWius);
    addLog('tab visible -> rejoined + snapshot');
  }

  function stop() {
    connection.value?.stop();
  }

  return {
    status,
    group,
    batchCount,
    connectionId,
    wiuState,
    log,
    start,
    stop,
    applySubscription,
    leaveGroup,
    rejoinGroup,
  };
}
