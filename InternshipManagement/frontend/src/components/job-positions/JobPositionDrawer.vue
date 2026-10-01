<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import Tag from 'primevue/tag'
import { canApply, daysLeft, formatDeadline, getPositionStatus } from '../../utils/jobPosition.js'

defineProps({
  visible: { type: Boolean, default: false },
  position: { type: Object, default: null },
  isAdmin: { type: Boolean, default: false }
})
const emit = defineEmits(['update:visible', 'apply'])
const isMobile = ref(false)
let mediaQuery = null

function onMediaChange(event) { isMobile.value = event.matches }
function deadlineCaption(position) {
  const remaining = daysLeft(position.deadline)
  if (remaining === null) return ''
  return remaining < 0 ? '· đã hết hạn' : `· còn ${remaining} ngày`
}
onMounted(() => {
  mediaQuery = window.matchMedia('(max-width: 767px)')
  isMobile.value = mediaQuery.matches
  mediaQuery.addEventListener('change', onMediaChange)
})
onBeforeUnmount(() => mediaQuery?.removeEventListener('change', onMediaChange))
</script>

<template>
  <Drawer :visible="visible" :position="isMobile ? 'bottom' : 'right'" class="position-detail-drawer" :style="isMobile ? { height: '85dvh' } : { width: 'min(520px, 100vw)' }" @update:visible="emit('update:visible', $event)">
    <template v-if="position">
      <span class="drawer-grab-handle" aria-hidden="true" />
      <header class="position-drawer-heading">
        <div><h2>{{ position.title }}</h2><p>{{ position.companyName }}</p></div>
        <Tag :value="getPositionStatus(position).label" :severity="getPositionStatus(position).severity" />
      </header>
      <dl class="position-drawer-meta">
        <div><dt>Phòng ban</dt><dd>{{ position.department || 'Chưa cập nhật' }}</dd></div>
        <div><dt>Địa điểm</dt><dd>{{ position.displayLocation || position.companyAddress || 'Chưa cập nhật' }}</dd></div>
        <div><dt>Hạn nộp hồ sơ</dt><dd>{{ formatDeadline(position.deadline) }} {{ deadlineCaption(position) }}</dd></div>
        <div><dt>Số lượng</dt><dd>{{ position.quantity }}</dd></div>
        <div v-if="isAdmin"><dt>Đã nhận/Tổng</dt><dd>{{ position.acceptedCount ?? 0 }}/{{ position.quantity }}</dd></div>
      </dl>
      <section class="position-drawer-description"><h3>Mô tả</h3><p>{{ position.description || 'Chưa có mô tả.' }}</p></section>
      <footer class="position-drawer-actions">
        <p v-if="isAdmin" class="position-drawer-note">Chỉ sinh viên mới gửi được yêu cầu thực tập.</p>
        <template v-else>
          <p v-if="!canApply(position)" class="position-apply-reason">{{ position.isExpired ? 'Vị trí đã hết hạn nhận hồ sơ.' : 'Vị trí đã đóng.' }}</p>
          <Button label="Ứng tuyển" icon="pi pi-send" :disabled="!canApply(position)" :title="position.isExpired ? 'Vị trí đã hết hạn' : 'Vị trí đã đóng'" @click="emit('apply', position)" />
        </template>
      </footer>
    </template>
  </Drawer>
</template>

<style scoped>
.position-drawer-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; padding: 4px 0 18px; border-bottom: 1px solid var(--border); }
.position-drawer-heading h2 { margin: 0 0 6px; color: var(--text-h); font-size: 20px; }
.position-drawer-heading p { margin: 0; color: var(--text); font-size: 14px; }
.position-drawer-meta { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; margin: 0; padding: 20px 0; border-bottom: 1px solid var(--border); }
.position-drawer-meta div { min-width: 0; }
.position-drawer-meta dt { margin-bottom: 4px; color: var(--text); font-size: 12px; }
.position-drawer-meta dd { overflow-wrap: anywhere; margin: 0; color: var(--text-h); font-size: 14px; }
.position-drawer-description { padding: 20px 0; }
.position-drawer-description h3 { margin: 0 0 8px; color: var(--text-h); font-size: 16px; }
.position-drawer-description p { margin: 0; color: var(--text); font-size: 14px; line-height: 1.6; white-space: pre-line; }
.drawer-grab-handle { display: none; }
:global(.position-detail-drawer .p-drawer-content) { display: flex; min-height: 0; flex: 1 1 auto; flex-direction: column; overflow-y: auto; }
.position-drawer-actions { position: sticky; z-index: 1; bottom: 0; flex: 0 0 auto; margin-top: auto; padding: 12px 0 max(12px, env(safe-area-inset-bottom)); background: var(--surface); border-top: 1px solid var(--border); }
.position-drawer-actions :deep(.p-button) { width: 100%; min-height: 44px; }
.position-apply-reason, .position-drawer-note { margin: 0 0 10px; color: var(--text); font-size: 13px; line-height: 1.4; }
@media (max-width: 767px) { .drawer-grab-handle { display: block; width: 38px; height: 4px; margin: 0 auto 12px; background: var(--border); border-radius: 999px; }.position-drawer-meta { grid-template-columns: minmax(0, 1fr); }.position-drawer-actions { padding-bottom: max(12px, env(safe-area-inset-bottom)); } }
</style>