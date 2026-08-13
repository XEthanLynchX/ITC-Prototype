<script setup>
defineProps({
  title: { type: String, required: true },
});
</script>

<template>
  <section class="panel">
    <h2>{{ title }}</h2>
    <slot />
  </section>
</template>

<style scoped>
.panel {
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--panel);
  padding: 12px 14px;
  margin-bottom: 14px;
}

h2 {
  color: var(--accent);
  font-size: 12px;
  font-weight: normal;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin: 0 0 10px;
}
</style>
