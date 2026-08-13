<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import PanelBox from './PanelBox.vue';

const props = defineProps({
  // The server's group name for this tab, so its own row can be highlighted.
  myGroup: { type: String, default: '(none)' },
});

const snapshot = ref(null);
const error = ref(null);
let timer = null;

async function poll() {
  try {
    const res = await fetch('/debug');
    snapshot.value = await res.json();
    error.value = null;
  } catch (err) {
    error.value = err.message;
  }
}

// Polling, not pushing: the registry view is a debugging aid, and giving it its own
// SignalR channel would mean the thing being observed changes what it observes.
onMounted(() => {
  poll();
  timer = setInterval(poll, 2000);
});

onUnmounted(() => clearInterval(timer));
</script>

<template>
  <PanelBox title="Subscription registry (all tabs)">
    <p v-if="error" class="error">/debug unreachable: {{ error }}</p>

    <template v-else-if="snapshot">
      <div class="counts">
        <span>Active connections: <b>{{ snapshot.activeConnections }}</b></span>
        <span>Unique groups: <b>{{ snapshot.uniqueGroups }}</b></span>
        <span>Pending changes: <b>{{ snapshot.pendingWiuChanges }}</b></span>
      </div>

      <div class="cols">
        <div>
          <h3>Groups</h3>
          <p v-if="snapshot.groups.length === 0" class="empty">none</p>
          <div
            v-for="g in snapshot.groups"
            :key="g.groupName"
            class="group"
            :class="{ mine: g.groupName === myGroup }"
          >
            <code>{{ g.groupName }}</code>
            <div class="meta">
              connections: {{ g.connectionCount }}
              <span v-if="g.groupName === myGroup" class="tag">this tab</span>
            </div>
          </div>
        </div>

        <div>
          <h3>Reverse index (WIU → groups)</h3>
          <p v-if="Object.keys(snapshot.reverseIndex).length === 0" class="empty">none</p>
          <div v-for="(groups, wiu) in snapshot.reverseIndex" :key="wiu" class="group">
            <code>{{ wiu }}</code>
            <div v-for="g in groups" :key="g" class="meta">→ {{ g }}</div>
          </div>
        </div>
      </div>
    </template>
  </PanelBox>
</template>

<style scoped>
.counts { display: flex; gap: 22px; flex-wrap: wrap; color: var(--muted); margin-bottom: 14px; }
.counts b { color: var(--text); }

.cols { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
@media (max-width: 900px) { .cols { grid-template-columns: 1fr; } }

h3 {
  font-size: 11px;
  font-weight: normal;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: .06em;
  margin: 0 0 8px;
}

.group { margin-bottom: 10px; word-break: break-all; }
.group.mine code { color: var(--ok); }
.meta { color: var(--muted); font-size: 12px; }
.tag {
  color: var(--ok);
  border: 1px solid #2f5b3c;
  border-radius: 3px;
  padding: 0 5px;
  margin-left: 6px;
  font-size: 11px;
}
.empty { color: #5e6874; }
.error { color: var(--bad); }
</style>
