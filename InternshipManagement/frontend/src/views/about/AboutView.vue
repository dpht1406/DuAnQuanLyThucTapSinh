<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import ProgressBar from 'primevue/progressbar'
import Skeleton from 'primevue/skeleton'
import Tag from 'primevue/tag'
import { getCompanyProfile } from '../../data/companyProfileSeed.js'
import { STATUS_SEVERITY } from '../../utils/studentStatus.js'

const route = useRoute()

const profile = ref(null)
const loading = ref(true)
const errorMessage = ref('')

const CHECKLIST_KEY = 'about_apply_checklist_v1'
const checked = ref([])

const sections = [
  { id: 'lich-su', label: 'Lịch sử' },
  { id: 'linh-vuc', label: 'Lĩnh vực' },
  { id: 'moi-truong', label: 'Môi trường' },
  { id: 'gia-tri', label: 'Giá trị cốt lõi' },
  { id: 'truoc-khi-ung-tuyen', label: 'Trước khi ứng tuyển' }
]

const openPositions = computed(() => profile.value?.positions.filter((p) => p.isOpen) ?? [])
const openSlots = computed(() => openPositions.value.reduce((sum, p) => sum + p.quantity, 0))
const checklistTotal = computed(() => profile.value?.studentInfo.checklist.length ?? 0)
const checklistPercent = computed(() =>
  checklistTotal.value === 0 ? 0 : Math.round((checked.value.length / checklistTotal.value) * 100)
)

function readChecklist() {
  try {
    const raw = localStorage.getItem(CHECKLIST_KEY)
    const parsed = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function saveChecklist(value) {
  try {
    localStorage.setItem(CHECKLIST_KEY, JSON.stringify(value))
  } catch {
    /* Trình duyệt chặn lưu trữ: bỏ qua, checklist vẫn dùng được trong phiên hiện tại. */
  }
}

async function loadProfile(retry = false) {
  loading.value = true
  errorMessage.value = ''
  try {
    // ?error=1 chỉ có tác dụng khi chạy dev, dùng để kiểm thử nhánh lỗi.
    const simulateError = import.meta.env.DEV && route.query.error === '1' && !retry
    profile.value = await getCompanyProfile({ simulateError })
    const validIds = new Set(profile.value.studentInfo.checklist.map((item) => item.id))
    checked.value = readChecklist().filter((id) => validIds.has(id))
  } catch (err) {
    profile.value = null
    errorMessage.value = err?.message || 'Không tải được thông tin doanh nghiệp.'
  } finally {
    loading.value = false
  }
}

function scrollToSection(id) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function resetChecklist() {
  checked.value = []
}

watch(checked, saveChecklist, { deep: true })
onMounted(loadProfile)
</script>

<template>
  <div class="page-container about-page">
    <!-- Trạng thái tải -->
    <div v-if="loading" class="about-loading" aria-busy="true" aria-live="polite">
      <Skeleton height="2.5rem" width="60%" class="mb-3" />
      <Skeleton height="1.25rem" width="90%" class="mb-2" />
      <Skeleton height="1.25rem" width="75%" class="mb-5" />
      <Skeleton height="14rem" borderRadius="14px" />
    </div>

    <!-- Trạng thái lỗi -->
    <div v-else-if="errorMessage" class="about-error" role="alert">
      <Message severity="error" :closable="false">{{ errorMessage }}</Message>
      <Button label="Thử lại" icon="pi pi-refresh" @click="loadProfile(true)" />
    </div>

    <template v-else-if="profile">
      <!-- Hero -->
      <section class="hero" aria-labelledby="about-title">
        <div class="hero-copy">
          <p class="hero-company">{{ profile.name }}</p>
          <h1 id="about-title" class="hero-title">{{ profile.tagline }}</h1>
          <p class="hero-summary">{{ profile.summary }}</p>
          <div class="hero-actions">
            <Button label="Xem vị trí đang mở" icon="pi pi-briefcase" @click="scrollToSection('truoc-khi-ung-tuyen')" />
            <router-link to="/companies" class="hero-link">Danh sách doanh nghiệp đối tác</router-link>
          </div>
        </div>

        <aside class="hero-facts" aria-label="Thông tin nhanh">
          <h2 class="facts-title">Thông tin nhanh</h2>
          <dl class="facts-list">
            <div v-for="fact in profile.facts" :key="fact.label" class="fact">
              <span :class="fact.icon" aria-hidden="true" />
              <dt>{{ fact.label }}</dt>
              <dd>{{ fact.value }}</dd>
            </div>
          </dl>
          <p class="facts-open">
            <strong>{{ openSlots }}</strong> chỉ tiêu đang mở ở <strong>{{ openPositions.length }}</strong> vị trí
          </p>
        </aside>
      </section>

      <nav class="section-nav" aria-label="Mục lục trang">
        <button v-for="s in sections" :key="s.id" type="button" class="section-chip" @click="scrollToSection(s.id)">
          {{ s.label }}
        </button>
      </nav>

      <!-- Lịch sử -->
      <section id="lich-su" class="section" aria-labelledby="h-lich-su">
        <h2 id="h-lich-su" class="section-title">Lịch sử hình thành</h2>
        <ol class="timeline">
          <li v-for="item in profile.history" :key="item.year" class="timeline-item">
            <span class="timeline-year">{{ item.year }}</span>
            <div class="timeline-body">
              <h3>{{ item.title }}</h3>
              <p>{{ item.text }}</p>
            </div>
          </li>
        </ol>
      </section>

      <!-- Lĩnh vực -->
      <section id="linh-vuc" class="section" aria-labelledby="h-linh-vuc">
        <h2 id="h-linh-vuc" class="section-title">Lĩnh vực hoạt động</h2>
        <div class="field-grid">
          <article v-for="f in profile.fields" :key="f.title" class="field-card">
            <span class="icon-badge" :class="f.icon" aria-hidden="true" />
            <h3>{{ f.title }}</h3>
            <p>{{ f.text }}</p>
            <ul class="tag-row" aria-label="Công nghệ sử dụng">
              <li v-for="t in f.tags" :key="t">{{ t }}</li>
            </ul>
          </article>
        </div>
      </section>

      <!-- Môi trường -->
      <section id="moi-truong" class="section" aria-labelledby="h-moi-truong">
        <h2 id="h-moi-truong" class="section-title">Môi trường làm việc</h2>
        <div class="env-layout">
          <div class="env-highlights">
            <article v-for="h in profile.environment.highlights" :key="h.title" class="env-item">
              <span class="icon-badge small" :class="h.icon" aria-hidden="true" />
              <div>
                <h3>{{ h.title }}</h3>
                <p>{{ h.text }}</p>
              </div>
            </article>
          </div>
          <div class="env-week">
            <h3>Một tuần thực tập diễn ra thế nào</h3>
            <dl>
              <div v-for="d in profile.environment.week" :key="d.day" class="week-row">
                <dt>{{ d.day }}</dt>
                <dd>{{ d.text }}</dd>
              </div>
            </dl>
            <p class="env-tools-label">Công cụ bạn sẽ dùng</p>
            <ul class="tag-row">
              <li v-for="t in profile.environment.tools" :key="t">{{ t }}</li>
            </ul>
          </div>
        </div>
      </section>

      <!-- Giá trị cốt lõi -->
      <section id="gia-tri" class="section" aria-labelledby="h-gia-tri">
        <h2 id="h-gia-tri" class="section-title">Giá trị cốt lõi</h2>
        <ul class="value-list">
          <li v-for="v in profile.values" :key="v.title" class="value-item">
            <span class="value-icon" :class="v.icon" aria-hidden="true" />
            <div>
              <h3>{{ v.title }}</h3>
              <p>{{ v.text }}</p>
            </div>
          </li>
        </ul>
      </section>

      <!-- Thông tin sinh viên -->
      <section id="truoc-khi-ung-tuyen" class="section" aria-labelledby="h-ung-tuyen">
        <h2 id="h-ung-tuyen" class="section-title">Sinh viên cần biết trước khi ứng tuyển</h2>

        <h3 class="sub-title">Vị trí đang nhận</h3>
        <ul class="position-list">
          <li v-for="p in profile.positions" :key="p.title" class="position-row" :class="{ closed: !p.isOpen }">
            <div class="position-main">
              <strong>{{ p.title }}</strong>
              <span>Cần biết: {{ p.skills }}</span>
            </div>
            <span class="position-qty">{{ p.quantity }} chỉ tiêu</span>
            <Tag :value="p.isOpen ? 'Đang mở' : 'Đã đủ'" :severity="p.isOpen ? 'success' : 'secondary'" />
          </li>
        </ul>

        <div class="info-grid">
          <div class="info-block">
            <h3 class="sub-title">Điều kiện</h3>
            <ul class="plain-list">
              <li v-for="e in profile.studentInfo.eligibility" :key="e">{{ e }}</li>
            </ul>
            <h3 class="sub-title">Hồ sơ cần chuẩn bị</h3>
            <ul class="plain-list">
              <li v-for="d in profile.studentInfo.documents" :key="d">{{ d }}</li>
            </ul>
          </div>

          <div class="info-block">
            <h3 class="sub-title">Quy trình từ lúc nộp đến khi hoàn thành</h3>
            <ol class="steps">
              <li v-for="(s, i) in profile.studentInfo.process" :key="s.status" class="step">
                <span class="step-index" aria-hidden="true">{{ i + 1 }}</span>
                <div>
                  <Tag :value="s.status" :severity="STATUS_SEVERITY[i + 1]" />
                  <p>{{ s.text }}</p>
                </div>
              </li>
            </ol>
          </div>
        </div>

        <div class="checklist-card">
          <div class="checklist-head">
            <div>
              <h3 class="sub-title flush">Kiểm tra nhanh trước khi gửi yêu cầu</h3>
              <p class="checklist-count">Đã hoàn thành {{ checked.length }}/{{ checklistTotal }} mục</p>
            </div>
            <Button label="Đặt lại" text size="small" :disabled="checked.length === 0" @click="resetChecklist" />
          </div>
          <ProgressBar :value="checklistPercent" :showValue="false" style="height: 8px" aria-label="Tiến độ chuẩn bị hồ sơ" />
          <ul class="check-list">
            <li v-for="item in profile.studentInfo.checklist" :key="item.id">
              <Checkbox v-model="checked" :inputId="`chk-${item.id}`" :value="item.id" />
              <label :for="`chk-${item.id}`">{{ item.label }}</label>
            </li>
          </ul>
          <p v-if="checklistPercent === 100" class="checklist-done" role="status">
            <span class="pi pi-check-circle" aria-hidden="true" /> Bạn đã sẵn sàng. Liên hệ phòng quản lý thực tập để được giới thiệu.
          </p>
        </div>

        <h3 class="sub-title">Câu hỏi thường gặp</h3>
        <Accordion value="0" class="faq">
          <AccordionPanel v-for="(item, i) in profile.studentInfo.faq" :key="item.q" :value="String(i)">
            <AccordionHeader>{{ item.q }}</AccordionHeader>
            <AccordionContent><p class="faq-answer">{{ item.a }}</p></AccordionContent>
          </AccordionPanel>
        </Accordion>

        <h3 class="sub-title">Liên hệ</h3>
        <address class="contact">
          <p><span class="pi pi-user" aria-hidden="true" />{{ profile.contact.person }}</p>
          <p><span class="pi pi-map-marker" aria-hidden="true" />{{ profile.contact.address }}</p>
          <p><span class="pi pi-envelope" aria-hidden="true" /><a :href="`mailto:${profile.contact.email}`">{{ profile.contact.email }}</a></p>
          <p><span class="pi pi-phone" aria-hidden="true" /><a :href="`tel:${profile.contact.phone.replace(/\s/g, '')}`">{{ profile.contact.phone }}</a></p>
          <p><span class="pi pi-clock" aria-hidden="true" />{{ profile.contact.hours }}</p>
        </address>
      </section>

      <p class="seed-note">Thông tin trên trang là dữ liệu minh họa của một doanh nghiệp giả lập.</p>
    </template>
  </div>
</template>

<style scoped>
.about-page { display: grid; gap: 40px; }
.about-loading, .about-error { display: grid; gap: 12px; justify-items: start; }
.about-loading :deep(.p-skeleton) { max-width: 100%; }
.mb-2 { margin-bottom: .5rem; } .mb-3 { margin-bottom: .75rem; } .mb-5 { margin-bottom: 1.5rem; }

/* Hero */
.hero { display: grid; grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr); gap: 32px; align-items: start; }
.hero-company { margin: 0 0 12px; color: var(--primary); font-size: var(--text-body); font-weight: 600; }
.hero-title { margin: 0; color: var(--text-h); font-size: 34px; font-weight: 700; line-height: 1.25; max-width: 24ch; letter-spacing: -0.01em; }
.hero-summary { margin: 16px 0 0; max-width: 62ch; font-size: 16px; line-height: 1.65; }
.hero-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 16px; margin-top: 24px; }
.hero-link { color: var(--primary); font-size: var(--text-body); font-weight: 600; text-decoration: none; }
.hero-link:hover { text-decoration: underline; }
.hero-facts { padding: 24px; background: var(--primary-tint); border: 1px solid #dfe2ff; border-radius: 20px; }
.facts-title { margin: 0 0 16px; color: var(--text-h); font-size: var(--text-card-title); font-weight: 600; }
.facts-list { display: grid; gap: 14px; margin: 0; }
.fact { display: grid; grid-template-columns: 22px 1fr; column-gap: 12px; align-items: baseline; }
.fact > span { grid-row: 1 / span 2; color: var(--primary); font-size: 16px; align-self: start; padding-top: 2px; }
.fact dt { color: var(--text); font-size: var(--text-caption); }
.fact dd { margin: 0; color: var(--text-h); font-size: var(--text-body); font-weight: 600; }
.facts-open { margin: 20px 0 0; padding-top: 16px; border-top: 1px solid #dfe2ff; color: var(--text-h); font-size: var(--text-body); }

/* Mục lục */
.section-nav { display: flex; gap: 8px; overflow-x: auto; padding-bottom: 4px; scrollbar-width: none; }
.section-nav::-webkit-scrollbar { display: none; }
.section-chip { flex: 0 0 auto; padding: 8px 14px; color: var(--text); background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-pill); font: inherit; font-size: 13px; font-weight: 500; cursor: pointer; }
.section-chip:hover { color: var(--primary); border-color: var(--primary); }
.section-chip:focus-visible, .hero-link:focus-visible { outline: 2px solid var(--primary); outline-offset: 2px; }

/* Section chung */
.section { display: grid; gap: 20px; scroll-margin-top: 84px; }
.section-title { margin: 0; color: var(--text-h); font-size: 24px; font-weight: 700; line-height: 1.3; }
.sub-title { margin: 8px 0 0; color: var(--text-h); font-size: var(--text-card-title); font-weight: 600; }
.sub-title.flush { margin: 0; }
h3 { margin: 0; }
p { margin: 0; }

.icon-badge { display: grid; place-items: center; width: 44px; height: 44px; color: var(--primary); background: var(--primary-tint); border-radius: 12px; font-size: 20px; }
.icon-badge.small { flex: 0 0 auto; width: 36px; height: 36px; font-size: 16px; }
.tag-row { display: flex; flex-wrap: wrap; gap: 6px; margin: 0; padding: 0; list-style: none; }
.tag-row li { padding: 3px 10px; color: var(--text-h); background: var(--surface-soft); border: 1px solid var(--border); border-radius: var(--radius-pill); font-size: var(--text-caption); font-weight: 500; }

/* Timeline (thứ tự thời gian thật nên dùng năm làm mốc) */
.timeline { position: relative; margin: 0; padding: 0; list-style: none; }
.timeline::before { content: ''; position: absolute; top: 8px; bottom: 8px; left: 51px; width: 2px; background: var(--border); }
.timeline-item { position: relative; display: grid; grid-template-columns: 52px 1fr; gap: 28px; padding: 0 0 24px; }
.timeline-item:last-child { padding-bottom: 0; }
.timeline-item::before { content: ''; position: absolute; top: 6px; left: 46px; width: 12px; height: 12px; background: var(--surface); border: 3px solid var(--primary); border-radius: 50%; }
.timeline-year { color: var(--primary); font-size: 18px; font-weight: 700; line-height: 1.3; }
.timeline-body { padding-left: 12px; }
.timeline-body h3 { color: var(--text-h); font-size: var(--text-card-title); font-weight: 600; }
.timeline-body p { margin-top: 4px; max-width: 64ch; line-height: 1.6; font-size: var(--text-body); }

/* Lĩnh vực */
.field-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.field-card { display: grid; align-content: start; gap: 10px; padding: 22px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.field-card h3 { color: var(--text-h); font-size: 17px; font-weight: 600; }
.field-card p { font-size: var(--text-body); line-height: 1.6; }

/* Môi trường */
.env-layout { display: grid; grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr); gap: 32px; }
.env-highlights { display: grid; gap: 20px; align-content: start; }
.env-item { display: flex; gap: 14px; }
.env-item h3 { color: var(--text-h); font-size: 15px; font-weight: 600; }
.env-item p { margin-top: 2px; font-size: var(--text-body); line-height: 1.55; }
.env-week { padding: 22px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.env-week h3 { color: var(--text-h); font-size: var(--text-card-title); font-weight: 600; }
.env-week dl { margin: 14px 0 0; }
.week-row { display: grid; grid-template-columns: 110px 1fr; gap: 12px; padding: 10px 0; border-bottom: 1px solid var(--border); font-size: var(--text-body); }
.week-row:last-child { border-bottom: 0; }
.week-row dt { color: var(--text-h); font-weight: 600; }
.week-row dd { margin: 0; line-height: 1.5; }
.env-tools-label { margin: 14px 0 8px; color: var(--text-h); font-size: var(--text-caption); font-weight: 600; }

/* Giá trị */
.value-list { display: grid; gap: 0; margin: 0; padding: 0; list-style: none; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); }
.value-item { display: flex; gap: 16px; padding: 18px 22px; border-bottom: 1px solid var(--border); }
.value-item:last-child { border-bottom: 0; }
.value-icon { flex: 0 0 auto; color: var(--primary); font-size: 20px; padding-top: 2px; }
.value-item h3 { color: var(--text-h); font-size: 15px; font-weight: 600; }
.value-item p { margin-top: 2px; font-size: var(--text-body); line-height: 1.55; }

/* Sinh viên */
.position-list { display: grid; gap: 8px; margin: 0; padding: 0; list-style: none; }
.position-row { display: grid; grid-template-columns: 1fr auto auto; align-items: center; gap: 16px; padding: 14px 18px; background: var(--surface); border: 1px solid var(--border); border-radius: 12px; }
.position-row.closed { background: var(--surface-soft); }
.position-row.closed .position-main strong { color: var(--text); }
.position-main { display: grid; gap: 2px; min-width: 0; }
.position-main strong { color: var(--text-h); font-size: 15px; font-weight: 600; }
.position-main span, .position-qty { font-size: 13px; }
.position-qty { white-space: nowrap; }

.info-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 32px; }
.info-block { display: grid; gap: 12px; align-content: start; }
.plain-list { display: grid; gap: 8px; margin: 0; padding-left: 20px; font-size: var(--text-body); line-height: 1.6; }
.plain-list li::marker { color: var(--primary); }

.steps { display: grid; gap: 14px; margin: 0; padding: 0; list-style: none; }
.step { display: flex; gap: 12px; }
.step-index { flex: 0 0 auto; display: grid; place-items: center; width: 26px; height: 26px; color: white; background: var(--primary); border-radius: 50%; font-size: 12px; font-weight: 600; }
.step p { margin-top: 6px; font-size: var(--text-body); line-height: 1.55; }

.checklist-card { display: grid; gap: 14px; padding: 22px; background: var(--surface); border: 1px solid var(--border); border-radius: 18px; }
.checklist-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
.checklist-count { margin-top: 2px; font-size: 13px; }
.check-list { display: grid; gap: 4px; margin: 0; padding: 0; list-style: none; }
.check-list li { display: flex; align-items: flex-start; gap: 12px; padding: 10px 0; }
.check-list label { color: var(--text-h); font-size: var(--text-body); line-height: 1.5; cursor: pointer; }
.checklist-done { display: flex; align-items: center; gap: 8px; padding: 12px 14px; color: #047857; background: #ecfdf5; border-radius: 10px; font-size: var(--text-body); font-weight: 500; }

.faq :deep(.p-accordionheader) { font-size: var(--text-body); font-weight: 600; }
.faq-answer { font-size: var(--text-body); line-height: 1.65; }

.contact { display: grid; gap: 10px; padding: 20px 22px; font-style: normal; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-md); font-size: var(--text-body); }
.contact p { display: flex; align-items: baseline; gap: 12px; }
.contact .pi { flex: 0 0 auto; width: 16px; color: var(--primary); }
.contact a { color: var(--primary); text-decoration: none; word-break: break-word; }
.contact a:hover { text-decoration: underline; }

.seed-note { color: var(--text); font-size: var(--text-caption); text-align: center; }

/* Tablet & mobile */
@media (max-width: 900px) {
  .hero { grid-template-columns: 1fr; gap: 24px; }
  .env-layout, .info-grid { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
  .about-page { gap: 32px; }
  .hero-title { font-size: 26px; }
  .section-title { font-size: 21px; }
  .field-grid { grid-template-columns: 1fr; }
  .hero-facts { padding: 20px; }
  .timeline::before { left: 5px; }
  .timeline-item { grid-template-columns: 1fr; gap: 2px; padding-left: 28px; }
  .timeline-item::before { left: 0; }
  .timeline-body { padding-left: 0; }
  .week-row { grid-template-columns: 1fr; gap: 2px; }
  .position-row { grid-template-columns: 1fr auto; row-gap: 8px; }
  .position-qty { grid-column: 1; grid-row: 2; }
  .position-row :deep(.p-tag) { grid-column: 2; grid-row: 1 / span 2; align-self: center; }
  .value-item { padding: 16px; }
}
@media (prefers-reduced-motion: reduce) {
  .section-chip { transition: none; }
}
</style>
