<script setup>
</script>

<template>
  <div class="form-action-bar">
    <div class="form-action-bar-inner">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.form-action-bar {
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid var(--ui-border);
}

.form-action-bar-inner {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
}

:deep(.p-button) {
  font-family: var(--ui-font-sans);
  font-size: 14px;
  font-weight: 500;
  border-radius: var(--ui-radius-sm);
  transition: var(--ui-transition);
}

:deep(.btn-cancel) {
  color: var(--ui-muted) !important;
  background: transparent !important;
  border: 1px solid transparent !important;
  box-shadow: none !important;
  padding: 8px 16px;
}

:deep(.btn-cancel:hover) {
  color: var(--ui-ink) !important;
  background: var(--ui-surface-sunken) !important;
  border-color: transparent !important;
}

:deep(.btn-primary) {
  background: var(--ui-primary-600) !important;
  border-color: var(--ui-primary-600) !important;
  color: #ffffff !important;
  box-shadow: none !important;
  padding: 8px 20px;
}

:deep(.btn-primary:hover) {
  background: var(--ui-primary-700) !important;
  border-color: var(--ui-primary-700) !important;
}

:deep(.btn-primary:focus-visible),
:deep(.btn-cancel:focus-visible) {
  outline: 2px solid var(--ui-primary-600) !important;
  outline-offset: 1px !important;
}

@media (max-width: 640px) {
  .form-action-bar {
    position: sticky;
    bottom: 0;
    left: 0;
    right: 0;
    z-index: 20;
    margin: 28px -1rem 0;
    padding: 12px 1rem max(12px, env(safe-area-inset-bottom));
    background: var(--ui-surface);
    border-top: 1px solid var(--ui-border);
  }

  .form-action-bar-inner {
    width: 100%;
    display: flex;
    gap: 10px;
  }

  :deep(.p-button),
  :deep(button) {
    flex: 1 1 0;
    min-height: 48px;
    justify-content: center;
  }
}
</style>
