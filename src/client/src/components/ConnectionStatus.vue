<script setup>
defineProps({
  status: { type: String, required: true },
  group: { type: String, required: true },
  batchCount: { type: Number, required: true },
  connectionId: { type: String, default: null },
});
</script>

<template>
  <div class="bar">
    Status: <span class="status" :class="status">{{ status }}</span>
    <span class="sep">·</span>
    Group: <code>{{ group }}</code>
    <span class="sep">·</span>
    <!-- data-testid is read by driver.mjs screenshot; renaming it breaks the driver. -->
    Batches received: <span class="count" data-testid="batch-count">{{ batchCount }}</span>
    <template v-if="connectionId">
      <span class="sep">·</span>
      <span class="muted">{{ connectionId }}</span>
    </template>
  </div>
</template>

<style scoped>
.bar { color: var(--muted); margin-bottom: 18px; }
.sep { margin: 0 10px; }
.count { color: var(--text); font-weight: bold; }
.muted { color: #5e6874; }

.status { font-weight: bold; }
.status.connected { color: var(--ok); }
.status.connecting,
.status.reconnecting { color: var(--warn); }
.status.disconnected { color: var(--bad); }
</style>
