<script setup>
import Button from 'primevue/button'

defineProps({
  title: { type: String, default: '' },
  index: { type: Number, required: true },
  total: { type: Number, required: true },
  canMoveUp: { type: Boolean, default: true },
  canMoveDown: { type: Boolean, default: true },
  disableActions: { type: Boolean, default: false }
})

const emit = defineEmits(['move-up', 'move-down', 'remove'])
</script>

<template>
  <div class="item-card">
    <div class="item-card-header">
      <span class="item-card-title">{{ title || `Mục ${index + 1}` }}</span>
      <div class="item-card-actions">
        <Button
          icon="pi pi-arrow-up"
          severity="secondary"
          text
          rounded
          size="small"
          :disabled="disableActions || index === 0 || !canMoveUp"
          :aria-label="`Di chuyển ${title || `mục ${index + 1}`} lên trên`"
          @click="emit('move-up')"
        />
        <Button
          icon="pi pi-arrow-down"
          severity="secondary"
          text
          rounded
          size="small"
          :disabled="disableActions || index === total - 1 || !canMoveDown"
          :aria-label="`Di chuyển ${title || `mục ${index + 1}`} xuống dưới`"
          @click="emit('move-down')"
        />
        <Button
          icon="pi pi-trash"
          severity="danger"
          text
          rounded
          size="small"
          :disabled="disableActions"
          :aria-label="`Xóa ${title || `mục ${index + 1}`}`"
          @click="emit('remove')"
        />
      </div>
    </div>
    <div class="item-card-body">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.item-card {
  padding: 16px;
  background: var(--surface-soft, #f8fafc);
  border: 1px solid var(--border, #e2e8f0);
  border-radius: var(--radius-md, 10px);
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.item-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid var(--border, #e2e8f0);
  padding-bottom: 8px;
}
.item-card-title {
  font-weight: 600;
  font-size: 14px;
  color: var(--text-h, #1e293b);
}
.item-card-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}
.item-card-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>
