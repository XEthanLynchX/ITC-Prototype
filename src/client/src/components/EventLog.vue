<script setup>
import PanelBox from './PanelBox.vue';

defineProps({
  entries: { type: Array, required: true },
});
</script>

<template>
  <PanelBox title="Event log">
    <div class="log">
      <div v-for="(entry, i) in entries" :key="`${entry.at}-${i}`" class="line">
        <span class="at">{{ entry.at }}</span>
        <span>{{ entry.message }}</span>
      </div>
      <div v-if="entries.length === 0" class="empty">nothing yet</div>
    </div>
  </PanelBox>
</template>

<style scoped>
.log {
  height: 240px;
  overflow-y: auto;
  font-size: 12px;
  color: #97a3b1;
}
.line { display: flex; gap: 14px; }
.at { color: #5e6874; flex: none; }
.empty { color: #5e6874; }
</style>
