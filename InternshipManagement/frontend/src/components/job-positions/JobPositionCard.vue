<script setup>
import Avatar from 'primevue/avatar'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import { canApply, daysLeft, formatDeadline, getPositionStatus } from '../../utils/jobPosition.js'

const props = defineProps({
  position: { type: Object, required: true },
  isAdmin: { type: Boolean, default: false }
})
const emit = defineEmits(['view', 'apply'])

function deadlineCaption(position) {
  const remaining = daysLeft(position.deadline)
  if (remaining === null) return ''
  if (remaining < 0) return 'đã hết hạn'
  return `còn ${remaining} ngày`
}
function applyTitle(position) {
  if (position.isExpired) return 'Vị trí đã hết hạn'
  if (!position.isOpen) return 'Vị trí đã đóng'
  return 'Ứng tuyển vị trí này'
}
</script>

<template>
  <article class="position-card" :class="{ 'position-card-inactive': !canApply(position) }" :aria-label="`${position.title} tại ${position.companyName}`">
    <div class="position-card-heading">
      <Avatar :label="(position.companyName || '?').trim().charAt(0).toUpperCase()" class="position-company-avatar" />
      <div class="position-heading-copy">
        <h2>{{ position.title }}</h2>
        <p>{{ position.companyName }}</p>
      </div>
    </div>
    <div class="position-status"><Tag :value="getPositionStatus(position).label" :severity="getPositionStatus(position).severity" /></div>
    <div class="position-details">
      <div class="position-detail-line"><i class="pi pi-sitemap" /><span>{{ position.department || 'Chưa cập nhật' }}</span></div>
      <div class="position-detail-line position-location"><i class="pi pi-map-marker" /><span>{{ position.displayLocation || position.companyAddress || 'Chưa cập nhật' }}</span></div>
      <div class="position-detail-line">
        <i class="pi pi-calendar" />
        <span>Hạn nộp: {{ formatDeadline(position.deadline) }}<small v-if="deadlineCaption(position)" :class="{ 'deadline-warning': daysLeft(position.deadline) >= 0 && daysLeft(position.deadline) <= 3, 'deadline-expired': daysLeft(position.deadline) < 0 }">&nbsp;· {{ deadlineCaption(position) }}</small></span>
      </div>
    </div>
    <footer class="position-card-footer">
      <Button label="Xem chi tiết" icon="pi pi-eye" text :aria-label="`Xem chi tiết ${position.title}`" @click="emit('view', position)" />
      <Button v-if="!isAdmin" label="Ứng tuyển" icon="pi pi-send" :disabled="!canApply(position)" :title="applyTitle(position)" :aria-label="`Ứng tuyển ${position.title}`" @click="emit('apply', position)" />
    </footer>
  </article>
</template>

<style scoped>
.position-card { display: flex; min-width: 0; min-height: 294px; flex-direction: column; gap: 14px; padding: 16px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); box-shadow: var(--shadow-sm); transition: box-shadow .15s ease, transform .15s ease; }
.position-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }
.position-card:focus-within { outline: 3px solid color-mix(in srgb, var(--primary) 35%, transparent); outline-offset: 2px; }
.position-card-heading { display: flex; align-items: center; gap: 12px; min-width: 0; }
.position-company-avatar { flex: 0 0 44px; width: 44px; height: 44px; color: var(--primary); background: var(--primary-tint); border-radius: 10px; font-weight: 700; }
.position-heading-copy { min-width: 0; }
.position-heading-copy h2 { display: -webkit-box; overflow: hidden; margin: 0 0 4px; color: var(--text-h); font-size: var(--text-card-title); line-height: 1.35; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.position-heading-copy p { overflow: hidden; margin: 0; color: var(--text); font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.position-card-inactive .position-heading-copy h2 { color: var(--text); }
.position-status { min-height: 25px; }
.position-details { display: grid; gap: 10px; color: var(--text); font-size: var(--text-body); line-height: 1.45; }
.position-detail-line { display: flex; align-items: flex-start; gap: 9px; min-width: 0; }
.position-detail-line > i { flex: 0 0 16px; padding-top: 3px; color: var(--primary); }
.position-detail-line > span { min-width: 0; }
.position-location > span { display: -webkit-box; overflow: hidden; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.position-detail-line small { font-size: 12px; }
.deadline-warning { color: var(--warning); }
.deadline-expired { color: var(--danger); }
.position-card-footer { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-top: auto; padding-top: 10px; border-top: 1px solid var(--border); }
.position-card-footer :deep(.p-button) { min-height: 44px; }
@media (max-width: 768px) { .position-card { width: 100%; min-height: 0; padding: 16px; }.position-card-footer { gap: 12px; }.position-card-footer :deep(.p-button) { flex: 1; min-height: 44px; } }
</style>