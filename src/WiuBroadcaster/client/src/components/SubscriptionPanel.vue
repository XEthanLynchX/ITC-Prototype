<script setup>
import { computed } from 'vue';
import PanelBox from './PanelBox.vue';

const props = defineProps({
  available: { type: Array, required: true },
  selected: { type: Array, required: true },
  connected: { type: Boolean, default: false },
});

const emit = defineEmits(['update:selected', 'apply', 'clear']);

// The group name the server *will* derive, mirrored client-side so the effect of
// ticking a box is visible before Apply is pressed.
const previewGroup = computed(() => {
  const wius = [...props.selected].sort();
  return wius.length ? `subscription:${wius.join('|')}` : 'subscription:none';
});

function toggle(wiu, checked) {
  const next = checked
    ? [...props.selected, wiu]
    : props.selected.filter((w) => w !== wiu);
  emit('update:selected', next);
}
</script>

<template>
  <PanelBox title="Subscribe to WIUs">
    <label v-for="wiu in available" :key="wiu" class="row">
      <input
        type="checkbox"
        :value="wiu"
        :checked="selected.includes(wiu)"
        @change="toggle(wiu, $event.target.checked)"
      />
      {{ wiu }}
    </label>

    <p class="preview">
      will join<br />
      <code>{{ previewGroup }}</code>
    </p>

    <button :disabled="!connected" @click="emit('apply')">Apply subscription</button>
    <button :disabled="!connected" @click="emit('clear')">Unsubscribe</button>
  </PanelBox>
</template>

<style scoped>
.row { display: block; padding: 3px 0; cursor: pointer; }
.preview {
  color: var(--muted);
  font-size: 12px;
  margin: 12px 0 6px;
  word-break: break-all;
}
</style>
