<script setup>
defineProps({
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    default: ''
  }
})
</script>

<template>
  <section class="form-section">
    <header class="form-section-header">
      <h2 class="form-section-title">{{ title }}</h2>
      <p v-if="description" class="form-section-description">{{ description }}</p>
    </header>
    <div class="form-section-body">
      <slot />
    </div>
  </section>
</template>

<style scoped>
.form-section {
  display: grid;
  grid-template-columns: 240px 1fr;
  column-gap: 32px;
  row-gap: 16px;
  padding: 28px 0;
  border-top: 1px solid var(--ui-border);
}

.form-section:first-child {
  border-top: none;
  padding-top: 0;
}

.form-section:last-child {
  padding-bottom: 0;
}

.form-section-header {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.form-section-title {
  margin: 0;
  font-family: var(--ui-font-sans);
  font-size: 15px;
  font-weight: 600;
  line-height: 1.4;
  color: var(--ui-ink);
}

.form-section-description {
  margin: 0;
  font-family: var(--ui-font-sans);
  font-size: 13px;
  line-height: 1.5;
  color: var(--ui-muted);
}

.form-section-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}

@media (max-width: 1023px) {
  .form-section {
    grid-template-columns: 1fr;
    row-gap: 16px;
    padding: 24px 0;
  }
}
</style>
