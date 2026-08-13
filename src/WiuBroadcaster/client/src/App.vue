<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useWiuConnection } from './useWiuConnection';
import ConnectionStatus from './components/ConnectionStatus.vue';
import SubscriptionPanel from './components/SubscriptionPanel.vue';
import WiuStateTable from './components/WiuStateTable.vue';
import EventLog from './components/EventLog.vue';
import RegistryPanel from './components/RegistryPanel.vue';

const {
  status, group, batchCount, connectionId, wiuState, log,
  start, stop, applySubscription, leaveGroup, rejoinGroup,
} = useWiuConnection();

// Fetched rather than hard-coded, so adding a WIU to appsettings.json shows up in
// the UI without touching the client.
const available = ref([]);
const selected = ref(['WIU-101', 'WIU-102']);
const leaveWhenHidden = ref(false);

const connected = computed(() => status.value === 'connected');

function onVisibilityChange() {
  if (!leaveWhenHidden.value) return;
  if (document.hidden) leaveGroup();
  else rejoinGroup();
}

onMounted(async () => {
  try {
    const config = await (await fetch('/debug/config')).json();
    available.value = config.wiuIds;
  } catch {
    available.value = ['WIU-101', 'WIU-102', 'WIU-103', 'WIU-104', 'WIU-105'];
  }

  await start();
  await applySubscription(selected.value);

  document.addEventListener('visibilitychange', onVisibilityChange);
});

onUnmounted(() => {
  document.removeEventListener('visibilitychange', onVisibilityChange);
  stop();
});
</script>

<template>
  <h1>Fake WIU Broadcaster</h1>

  <ConnectionStatus
    :status="status"
    :group="group"
    :batch-count="batchCount"
    :connection-id="connectionId"
  />

  <div class="layout">
    <div>
      <SubscriptionPanel
        v-model:selected="selected"
        :available="available"
        :connected="connected"
        @apply="applySubscription(selected)"
        @clear="selected = []; applySubscription([])"
      />

      <label class="hidden-toggle">
        <input v-model="leaveWhenHidden" type="checkbox" />
        Leave group when tab is hidden
      </label>
    </div>

    <div>
      <WiuStateTable :wiu-state="wiuState" />
      <RegistryPanel :my-group="group" />
      <EventLog :entries="log" />
    </div>
  </div>
</template>

<style scoped>
.layout {
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr);
  gap: 20px;
  align-items: start;
}

@media (max-width: 900px) {
  .layout { grid-template-columns: 1fr; }
}

.hidden-toggle {
  display: block;
  color: var(--muted);
  font-size: 12px;
  padding-left: 2px;
  cursor: pointer;
}
</style>
