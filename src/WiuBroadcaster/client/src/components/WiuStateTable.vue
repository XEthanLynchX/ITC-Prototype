<script setup>
import { computed } from 'vue';
import PanelBox from './PanelBox.vue';

const props = defineProps({
  wiuState: { type: Object, required: true },
});

const rows = computed(() =>
  Object.keys(props.wiuState)
    .sort()
    .map((wiuId) => ({ wiuId, ...props.wiuState[wiuId] }))
);
</script>

<template>
  <PanelBox title="Live WIU state">
    <table>
      <thead>
        <tr>
          <th>WIU</th>
          <th>Aspect</th>
          <th>Seq</th>
          <th>Observed (UTC)</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="rows.length === 0">
          <td colspan="4" class="empty">no data yet</td>
        </tr>
        <!-- Keyed by WIU id so Vue patches the row in place rather than rebuilding
             the table, which is the same by-id patching the server's batches assume. -->
        <tr v-for="row in rows" :key="row.wiuId">
          <td>{{ row.wiuId }}</td>
          <td :class="`aspect-${row.aspect}`">{{ row.aspect }}</td>
          <td>{{ row.sequence }}</td>
          <td>{{ row.observedAtUtc?.substring(11, 23) }}</td>
        </tr>
      </tbody>
    </table>
  </PanelBox>
</template>

<style scoped>
table { border-collapse: collapse; width: 100%; }

th, td {
  text-align: left;
  padding: 5px 10px;
  border-bottom: 1px solid #232a34;
}

th {
  color: var(--muted);
  font-weight: normal;
  font-size: 12px;
  text-transform: uppercase;
}

.empty { color: #5e6874; }

.aspect-Clear { color: var(--ok); }
.aspect-Approach { color: var(--warn); }
.aspect-Restricting { color: var(--alert); }
.aspect-Stop { color: var(--bad); }
</style>
