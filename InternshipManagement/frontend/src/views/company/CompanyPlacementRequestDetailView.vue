<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Message from 'primevue/message'
import { getCompanyPlacementRequestById } from '../../api/company-me.js'
import { extractErrorMessage } from '../../utils/apiError.js'
import { formatServerDateTime } from '../../utils/dateTime.js'
import {
  getEmbedCvUrl,
  mapPlacementRequestDueStatus,
  mapPlacementRequestStatus
} from '../../utils/companyPlacementRequest.js'

const route = useRoute()
const router = useRouter()
const detail = ref(null)
const loading = ref(false)
const error = ref('')
const showFullscreenCvModal = ref(false)

const statusInfo = computed(() => mapPlacementRequestStatus(detail.value?.status))
const dueStatus = computed(() => mapPlacementRequestDueStatus(detail.value?.dueAt))
const canRespond = computed(() => statusInfo.value?.value === 0)
const canRecordResult = computed(() => statusInfo.value?.value === 3)
const embedCvUrl = computed(() => getEmbedCvUrl(detail.value?.cvUrl))

const statusBadgeClass = computed(() => {
  const val = statusInfo.value?.value
  if (val === 0) return 'status-pending'
  if (val === 3) return 'status-interview'
  if (val === 4) return 'status-accepted'
  if (val === 2) return 'status-rejected'
  if (val === 5) return 'status-expired'
  return 'status-unknown'
})

function applicantName(item) {
  return item?.applicantFullName || item?.studentFullName || '—'
}

function dateLabel(value) {
  return formatServerDateTime(value) ?? '—'
}

async function loadDetail() {
  loading.value = true
  error.value = ''
  detail.value = null
  showFullscreenCvModal.value = false
  try {
    detail.value = await getCompanyPlacementRequestById(route.params.id)
  } catch (err) {
    error.value = extractErrorMessage(err, 'Không tải được chi tiết đơn ứng tuyển.')
  } finally {
    loading.value = false
  }
}

watch(() => route.params.id, loadDetail, { immediate: true })
</script>

<template>
  <div class="company-placement-detail-page">
    <div class="detail-page-inner">
      <!-- 1. Top Navigation: Back to list -->
      <div class="top-nav-bar">
        <router-link :to="{ name: 'company-placement-requests' }" class="back-link">
          <i class="pi pi-arrow-left" aria-hidden="true" />
          <span>Quay lại danh sách</span>
        </router-link>
      </div>

      <Message v-if="error" severity="error" :closable="false" class="error-banner">
        {{ error }}
      </Message>

      <div v-if="loading" class="loading-state">
        <i class="pi pi-spin pi-spinner" aria-hidden="true" />
        <span>Đang tải thông tin đơn ứng tuyển...</span>
      </div>

      <div v-else-if="detail" class="detail-content">
        <!-- Page Title & Subtitle (outside cards, on page canvas) -->
        <header class="page-header-block">
          <h1 class="page-title">Đơn ứng tuyển #{{ detail.id }}</h1>
          <p class="page-subtitle">Kiểm tra thông tin ứng viên, xem trước CV và thực hiện xét duyệt.</p>
        </header>

        <!-- Card 1: Trạng thái đơn ứng tuyển -->
        <div class="status-card">
          <div class="status-col status-col-state">
            <span class="status-label">Trạng thái</span>
            <span :class="['status-badge', statusBadgeClass]">
              {{ statusInfo.label }}
            </span>
          </div>

          <div class="status-divider" />

          <div class="status-col status-col-due">
            <span class="status-label">
              <i class="pi pi-clock" aria-hidden="true" />
              <span>Hạn phản hồi</span>
            </span>
            <span class="status-val">{{ detail.dueAt ? dateLabel(detail.dueAt) : '—' }}</span>
          </div>

          <div class="status-divider" />

          <div class="status-col status-col-created">
            <span class="status-label">
              <i class="pi pi-calendar" aria-hidden="true" />
              <span>Ngày nộp</span>
            </span>
            <span class="status-val">{{ dateLabel(detail.createdAt) }}</span>
          </div>
        </div>

        <!-- Card 2: Thông tin ứng viên -->
        <section class="content-card applicant-card">
          <div class="card-header">
            <h2 class="card-title">Thông tin ứng viên</h2>
            <p class="card-desc">Hồ sơ cá nhân và vị trí đăng ký ứng tuyển</p>
          </div>
          <div class="card-divider" />

          <div class="applicant-grid">
            <!-- 1. Họ tên ứng viên -->
            <div class="applicant-field field-name">
              <span class="field-label">Họ tên ứng viên</span>
              <span class="field-val">{{ applicantName(detail) }}</span>
            </div>

            <!-- 2. Vị trí ứng tuyển -->
            <div class="applicant-field field-job">
              <span class="field-label">Vị trí ứng tuyển</span>
              <span class="field-val">{{ detail.jobPositionTitle || 'Tuyển thực tập sinh ptpm' }}</span>
            </div>

            <!-- 3. Email -->
            <div class="applicant-field field-email">
              <span class="field-label">Email</span>
              <span class="field-val text-break">{{ detail.applicantEmail || '—' }}</span>
            </div>

            <!-- 4. Số điện thoại -->
            <div class="applicant-field field-phone">
              <span class="field-label">Số điện thoại</span>
              <span class="field-val">{{ detail.applicantPhone || '—' }}</span>
            </div>

            <!-- 5. Trường đào tạo -->
            <div class="applicant-field field-school">
              <span class="field-label">Trường đào tạo</span>
              <span class="field-val">{{ detail.applicantSchool || '—' }}</span>
            </div>

            <!-- 6. Ngành học -->
            <div class="applicant-field field-major">
              <span class="field-label">Ngành học</span>
              <span class="field-val">{{ detail.applicantMajor || detail.major || '—' }}</span>
            </div>

            <!-- 7. Lớp -->
            <div class="applicant-field field-class">
              <span class="field-label">Lớp</span>
              <span class="field-val">{{ detail.className || '—' }}</span>
            </div>

            <!-- 8. Thư giới thiệu -->
            <div class="applicant-field field-letter">
              <span class="field-label">Thư giới thiệu</span>
              <div class="letter-box">
                {{ detail.coverLetter || detail.note || 'tedjsfjdjklfjasdfslfajaisf' }}
              </div>
            </div>
          </div>
        </section>

        <!-- Card 3: Hồ sơ CV -->
        <section class="content-card cv-section-card">
          <!-- Desktop View (>= 768px) -->
          <div class="cv-desktop-layout">
            <div class="cv-info-col">
              <h2 class="card-title">Hồ sơ CV</h2>
              <p class="card-desc cv-col-desc">Xem trực tiếp bản CV được đính kèm hoặc mở qua tab mới.</p>

              <div class="cv-action-group">
                <div class="cv-badge-pill">
                  <i class="pi pi-file-pdf" aria-hidden="true" />
                  <span>Hồ sơ năng lực (CV)</span>
                </div>
                <a
                  v-if="detail.cvUrl"
                  :href="detail.cvUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="cv-open-link"
                >
                  <i class="pi pi-external-link" aria-hidden="true" />
                  <span>Mở CV trong tab mới</span>
                </a>
              </div>

              <div class="cv-preview-footnote">Bản xem trước</div>
            </div>

            <div class="cv-preview-col">
              <div v-if="detail.cvUrl" class="cv-preview-box">
                <div class="cv-iframe-container">
                  <iframe
                    :src="embedCvUrl"
                    title="Khung xem trước CV ứng viên"
                    class="cv-iframe"
                    loading="lazy"
                  />
                </div>
                <div class="cv-box-meta">
                  <h3 class="box-meta-title">CV đính kèm</h3>
                  <p class="box-meta-desc">
                    Bản xem trước được giữ trong trang để công ty kiểm tra nhanh hồ sơ.
                  </p>
                </div>
              </div>
              <div v-else class="empty-cv-box">
                <i class="pi pi-info-circle" aria-hidden="true" />
                <span>Ứng viên chưa cung cấp liên kết CV.</span>
              </div>
            </div>
          </div>

          <!-- Mobile View (< 768px) -->
          <div class="cv-mobile-layout">
            <div class="cv-mobile-header">
              <h2 class="card-title">Hồ sơ CV</h2>
              <p class="card-desc">Hồ sơ năng lực (CV)</p>
            </div>

            <div v-if="detail.cvUrl" class="cv-mobile-content">
              <div class="cv-mobile-thumb-row">
                <div
                  class="cv-mobile-thumb"
                  role="button"
                  tabindex="0"
                  aria-label="Xem CV toàn màn hình"
                  @click="showFullscreenCvModal = true"
                  @keydown.enter="showFullscreenCvModal = true"
                >
                  <iframe
                    :src="embedCvUrl"
                    title="CV Thu nhỏ"
                    class="cv-thumb-iframe"
                    tabindex="-1"
                  />
                  <div class="cv-thumb-mask" />
                </div>
                <div class="cv-mobile-meta">
                  <h3 class="box-meta-title">CV đính kèm</h3>
                  <p class="box-meta-desc">
                    Mở bản CV để xem đầy đủ nội dung và chi tiết kinh nghiệm.
                  </p>
                </div>
              </div>

              <div class="cv-mobile-actions">
                <button
                  type="button"
                  class="btn-primary-fullscreen"
                  @click="showFullscreenCvModal = true"
                >
                  <i class="pi pi-window-maximize" aria-hidden="true" />
                  <span>Xem CV toàn màn hình</span>
                </button>
                <a
                  :href="detail.cvUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="cv-mobile-open-link"
                >
                  Mở CV trong tab mới
                </a>
              </div>
            </div>

            <div v-else class="empty-cv-box">
              <i class="pi pi-info-circle" aria-hidden="true" />
              <span>Ứng viên chưa cung cấp liên kết CV.</span>
            </div>
          </div>
        </section>

        <!-- Optional: Thông tin xử lý (nếu có dữ liệu lịch sử) -->
        <section
          v-if="detail.interviewAt || detail.interviewLocation || detail.interviewNote || detail.rejectReason || detail.resultNote"
          class="content-card process-card"
        >
          <div class="card-header">
            <h2 class="card-title">Thông tin xử lý</h2>
            <p class="card-desc">Lịch sử tương tác và ghi nhận của doanh nghiệp.</p>
          </div>
          <div class="card-divider" />
          <div class="process-grid">
            <div v-if="detail.interviewAt" class="process-field">
              <span class="field-label">Lịch phỏng vấn</span>
              <span class="field-val">{{ dateLabel(detail.interviewAt) }}</span>
            </div>
            <div v-if="detail.interviewLocation" class="process-field">
              <span class="field-label">Địa điểm phỏng vấn</span>
              <span class="field-val">{{ detail.interviewLocation }}</span>
            </div>
            <div v-if="detail.interviewNote" class="process-field full-width">
              <span class="field-label">Ghi chú phỏng vấn</span>
              <div class="letter-box">{{ detail.interviewNote }}</div>
            </div>
            <div v-if="detail.rejectReason" class="process-field full-width">
              <span class="field-label">Lý do từ chối</span>
              <div class="letter-box">{{ detail.rejectReason }}</div>
            </div>
            <div v-if="detail.resultNote" class="process-field full-width">
              <span class="field-label">Ghi chú kết quả</span>
              <div class="letter-box">{{ detail.resultNote }}</div>
            </div>
          </div>
        </section>

        <!-- Card 4: Xét duyệt đơn -->
        <section class="content-card action-card">
          <div class="action-card-layout">
            <div class="action-aside">
              <h2 class="card-title">Xét duyệt đơn</h2>
              <p class="card-desc">Thực hiện chuyển đổi trạng thái hồ sơ ứng tuyển.</p>
            </div>

            <div class="action-divider" />

            <div class="action-main">
              <div class="action-status-info">
                <template v-if="canRespond">
                  <h3 class="action-status-title">Đơn đang chờ xử lý</h3>
                  <p class="action-status-desc">Bạn có thể gửi lời mời phỏng vấn hoặc từ chối đơn này:</p>
                </template>
                <template v-else-if="canRecordResult">
                  <h3 class="action-status-title">Đơn đã được mời phỏng vấn</h3>
                  <p class="action-status-desc">Bạn có thể ghi nhận kết quả đánh giá sau phỏng vấn:</p>
                </template>
                <template v-else-if="statusInfo.key === 'Accepted' || statusInfo.value === 4">
                  <h3 class="action-status-title">Đơn đã được tiếp nhận</h3>
                  <p class="action-status-desc">Ứng viên đã được tiếp nhận vào vị trí thực tập. Không cần thao tác thêm.</p>
                </template>
                <template v-else-if="statusInfo.key === 'Rejected' || statusInfo.value === 2">
                  <h3 class="action-status-title">Đơn đã bị từ chối</h3>
                  <p class="action-status-desc">Hồ sơ ứng tuyển này đã bị từ chối tiếp nhận. Không cần thao tác thêm.</p>
                </template>
                <template v-else>
                  <h3 class="action-status-title">Đơn ở trạng thái {{ statusInfo.label }}</h3>
                  <p class="action-status-desc">Hồ sơ ứng tuyển hiện đang ở trạng thái {{ statusInfo.label }}.</p>
                </template>
              </div>

              <!-- Button actions -->
              <div v-if="canRespond" class="action-buttons-wrap">
                <button
                  type="button"
                  class="btn-action-invite"
                  @click="router.push(`/company/placement-requests/${detail.id}/invite`)"
                >
                  <i class="pi pi-calendar-plus" aria-hidden="true" />
                  <span>Mời phỏng vấn</span>
                </button>
                <button
                  type="button"
                  class="btn-action-reject"
                  @click="router.push(`/company/placement-requests/${detail.id}/reject`)"
                >
                  <i class="pi pi-times" aria-hidden="true" />
                  <span>Từ chối đơn</span>
                </button>
              </div>

              <div v-else-if="canRecordResult" class="action-buttons-wrap">
                <button
                  type="button"
                  class="btn-action-invite"
                  @click="router.push(`/company/placement-requests/${detail.id}/result`)"
                >
                  <i class="pi pi-check-circle" aria-hidden="true" />
                  <span>Ghi nhận kết quả</span>
                </button>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>

    <!-- Teleport Fullscreen Modal for CV (Accessible on mobile & desktop) -->
    <Teleport to="body">
      <div
        v-if="showFullscreenCvModal"
        class="cv-fullscreen-overlay"
        role="dialog"
        aria-modal="true"
        aria-label="Xem trước CV toàn màn hình"
      >
        <div class="cv-fullscreen-bar">
          <div class="fullscreen-title">
            <i class="pi pi-file-pdf" aria-hidden="true" />
            <span>Hồ sơ năng lực (CV)</span>
          </div>
          <button
            type="button"
            class="fullscreen-close-btn"
            @click="showFullscreenCvModal = false"
            aria-label="Đóng"
          >
            Đóng (✕)
          </button>
        </div>
        <div class="cv-fullscreen-content">
          <iframe
            :src="embedCvUrl"
            title="Khung xem trước CV toàn màn hình"
            class="cv-fullscreen-iframe"
            loading="lazy"
          />
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.company-placement-detail-page {
  width: 100%;
  min-height: 100vh;
  background: var(--ui-canvas, #f6f4ee);
  padding: 24px 20px 60px;
  box-sizing: border-box;
}

.detail-page-inner {
  max-width: 1040px;
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
}

/* 1. Top Navigation: Back to list */
.top-nav-bar {
  margin-bottom: 12px;
}

.back-link {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 500;
  color: #5f655f;
  text-decoration: none;
  transition: color 0.15s ease;
}

.back-link:hover {
  color: var(--ui-primary-600, #1f6650);
}

.back-link i {
  font-size: 13px;
}

.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 4rem 1rem;
  color: #5f655f;
  font-size: 14px;
}

.error-banner {
  margin-bottom: 20px;
}

/* Page Header Block */
.page-header-block {
  margin-bottom: 18px;
}

.page-title {
  margin: 0 0 6px;
  font-size: 28px;
  font-weight: 700;
  color: #1c201e;
  line-height: 1.25;
}

.page-subtitle {
  margin: 0;
  font-size: 14px;
  color: #5f655f;
  line-height: 1.5;
}

/* General Content Card */
.content-card {
  background: #ffffff;
  border: 1px solid #e2ded3;
  border-radius: 12px;
  padding: 24px;
  box-sizing: border-box;
  margin-bottom: 20px;
  width: 100%;
}

.card-header {
  min-width: 0;
}

.card-title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
  color: #1c201e;
  line-height: 1.3;
}

.card-desc {
  margin: 4px 0 0;
  font-size: 13.5px;
  color: #5f655f;
  line-height: 1.45;
}

.card-divider {
  height: 1px;
  background: #eae6dd;
  margin: 16px 0 20px;
}

/* Card 1: Trạng thái đơn ứng tuyển */
.status-card {
  background: #ffffff;
  border: 1px solid #e2ded3;
  border-radius: 12px;
  padding: 16px 24px;
  display: flex;
  align-items: center;
  gap: 32px;
  box-sizing: border-box;
  margin-bottom: 20px;
  width: 100%;
}

.status-col {
  display: flex;
  align-items: center;
  gap: 12px;
}

.status-label {
  font-size: 13.5px;
  color: #5f655f;
  font-weight: 500;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
}

.status-label i {
  color: #5f655f;
  font-size: 14px;
}

.status-val {
  font-size: 14px;
  font-weight: 700;
  color: #1c201e;
  white-space: nowrap;
}

.status-divider {
  width: 1px;
  height: 26px;
  background: #e2ded3;
  flex-shrink: 0;
}

/* Status Badges */
.status-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: fit-content;
  padding: 4px 12px;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.2;
  white-space: nowrap;
  box-sizing: border-box;
}

.status-badge.status-pending {
  background: #fef3c7;
  color: #b45309;
}

.status-badge.status-interview {
  background: #dbeafe;
  color: #1e40af;
}

.status-badge.status-accepted {
  background: #dcfce7;
  color: #15803d;
}

.status-badge.status-rejected {
  background: #fee2e2;
  color: #b91c1c;
}

.status-badge.status-expired,
.status-badge.status-unknown {
  background: #f3f4f6;
  color: #4b5563;
}

/* Card 2: Thông tin ứng viên Grid (Desktop 3 columns) */
.applicant-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  column-gap: 32px;
  row-gap: 20px;
  width: 100%;
  box-sizing: border-box;
}

.field-name { grid-column: 1; grid-row: 1; }
.field-job { grid-column: 2; grid-row: 1; }
.field-school { grid-column: 3; grid-row: 1; }

.field-email { grid-column: 1; grid-row: 2; }
.field-phone { grid-column: 2; grid-row: 2; }
.field-major { grid-column: 3; grid-row: 2; }

.field-class { grid-column: 1; grid-row: 3; }
.field-letter { grid-column: 2 / span 2; grid-row: 3; }

.applicant-field {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.field-label {
  font-size: 13px;
  color: #5f655f;
  font-weight: 500;
  margin-bottom: 4px;
}

.field-val {
  font-size: 15px;
  font-weight: 700;
  color: #1c201e;
  line-height: 1.4;
  word-break: break-word;
}

.text-break {
  overflow-wrap: anywhere;
  word-break: break-word;
}

.letter-box {
  background: #faf8f5;
  border: 1px solid #ece8df;
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 14px;
  font-weight: 500;
  color: #1c201e;
  line-height: 1.5;
  min-height: 48px;
  box-sizing: border-box;
  word-break: break-word;
  white-space: pre-wrap;
}

/* Card 3: Hồ sơ CV (Desktop) */
.cv-desktop-layout {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  gap: 28px;
  width: 100%;
  box-sizing: border-box;
}

.cv-info-col {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.cv-col-desc {
  margin-top: 6px;
  margin-bottom: 20px;
}

.cv-action-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 24px;
}

.cv-badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #faf8f5;
  border: 1px solid #eae6dd;
  border-radius: 8px;
  padding: 9px 13px;
  font-size: 13.5px;
  font-weight: 600;
  color: #1c201e;
  width: fit-content;
}

.cv-badge-pill i {
  color: #1f6650;
  font-size: 16px;
}

.cv-open-link {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1px solid #eae6dd;
  border-radius: 8px;
  padding: 9px 13px;
  font-size: 13.5px;
  font-weight: 500;
  color: #1c201e;
  text-decoration: none;
  width: fit-content;
  transition: all 0.15s ease;
}

.cv-open-link:hover {
  background: #faf8f5;
  border-color: #cfcabc;
}

.cv-open-link i {
  color: #1f6650;
  font-size: 13.5px;
}

.cv-preview-footnote {
  font-size: 12.5px;
  color: #9a9d95;
  font-weight: 500;
  margin-top: auto;
}

.cv-preview-box {
  background: #faf9f6;
  border: 1px solid #eae6dd;
  border-radius: 12px;
  padding: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 28px;
  min-height: 460px;
  box-sizing: border-box;
}

.cv-iframe-container {
  width: 360px;
  height: 480px;
  max-width: 50%;
  border: 1px solid #cfcabc;
  border-radius: 8px;
  overflow: hidden;
  background: #ffffff;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  flex-shrink: 0;
}

.cv-iframe {
  width: 100%;
  height: 100%;
  border: none;
  display: block;
}

.cv-box-meta {
  flex: 1;
  max-width: 240px;
}

.box-meta-title {
  margin: 0 0 6px;
  font-size: 16px;
  font-weight: 700;
  color: #1c201e;
}

.box-meta-desc {
  margin: 0;
  font-size: 13.5px;
  color: #5f655f;
  line-height: 1.5;
}

.empty-cv-box {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 24px;
  background: #faf8f5;
  border: 1px dashed #ece8df;
  border-radius: 12px;
  color: #5f655f;
  font-size: 14px;
}

.cv-mobile-layout {
  display: none;
}

/* Card 4: Xét duyệt đơn (Desktop) */
.action-card-layout {
  display: flex;
  align-items: center;
  gap: 32px;
  width: 100%;
  box-sizing: border-box;
}

.action-aside {
  width: 240px;
  flex-shrink: 0;
}

.action-divider {
  width: 1px;
  align-self: stretch;
  background: #eae6dd;
  flex-shrink: 0;
}

.action-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.action-status-title {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
  color: #1c201e;
}

.action-status-desc {
  margin: 4px 0 0;
  font-size: 13.5px;
  color: #5f655f;
  line-height: 1.45;
}

.action-buttons-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.btn-action-invite {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: var(--ui-primary-600, #1f6650);
  color: #ffffff;
  border: 1px solid var(--ui-primary-600, #1f6650);
  border-radius: 8px;
  padding: 10px 22px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
  box-sizing: border-box;
}

.btn-action-invite:hover {
  background: var(--ui-primary-700, #19523f);
  border-color: var(--ui-primary-700, #19523f);
}

.btn-action-reject {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: #ffffff;
  color: #dc2626;
  border: 1px solid #fca5a5;
  border-radius: 8px;
  padding: 10px 22px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
  box-sizing: border-box;
}

.btn-action-reject:hover {
  background: #fef2f2;
  border-color: #f87171;
}

/* Process Grid (Thông tin xử lý nếu có) */
.process-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  width: 100%;
}

.process-field {
  display: flex;
  flex-direction: column;
}

.process-field.full-width {
  grid-column: 1 / -1;
}

/* Fullscreen CV Teleport Modal */
.cv-fullscreen-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: #ffffff;
  display: flex;
  flex-direction: column;
}

.cv-fullscreen-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: #1c201e;
  color: #ffffff;
}

.fullscreen-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
}

.fullscreen-close-btn {
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: #ffffff;
  border-radius: 8px;
  padding: 6px 14px;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
}

.cv-fullscreen-content {
  flex: 1;
  width: 100%;
  height: 100%;
  background: #525659;
}

.cv-fullscreen-iframe {
  width: 100%;
  height: 100%;
  border: none;
  display: block;
}

/* Tablet Breakpoint (768px - 1024px) */
@media (min-width: 768px) and (max-width: 1024px) {
  .cv-desktop-layout {
    grid-template-columns: 1fr;
    gap: 18px;
  }

  .cv-preview-box {
    flex-direction: column;
    text-align: center;
    gap: 16px;
  }

  .cv-iframe-container {
    width: 100%;
    max-width: 440px;
    height: 440px;
  }

  .cv-box-meta {
    max-width: 100%;
  }

  .action-card-layout {
    flex-direction: column;
    align-items: stretch;
    gap: 16px;
  }

  .action-aside {
    width: 100%;
  }

  .action-divider {
    display: none;
  }

  .applicant-grid {
    column-gap: 20px;
    row-gap: 16px;
  }
}

/* Mobile Breakpoint (< 768px) */
@media (max-width: 767px) {
  .company-placement-detail-page {
    padding: 16px 14px 40px;
    overflow-x: hidden;
  }

  .page-header-block {
    margin-bottom: 14px;
  }

  .page-title {
    font-size: 20px;
    line-height: 1.3;
  }

  .page-subtitle {
    font-size: 13px;
    margin-top: 4px;
  }

  /* Card 1: Trạng thái mobile (1 cột) */
  .status-card {
    flex-direction: column;
    align-items: stretch;
    padding: 16px;
    gap: 0;
    border-radius: 12px;
    margin-bottom: 14px;
  }

  .status-col-state {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-bottom: 12px;
  }

  .status-col-due {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
    padding: 12px 0;
  }

  .status-col-created {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
    padding-top: 12px;
  }

  .status-divider {
    width: 100%;
    height: 1px;
    background: #eae6dd;
    margin: 0;
  }

  .status-label {
    font-size: 13px;
  }

  .status-val {
    font-size: 14px;
    font-weight: 700;
  }

  /* Content Card on mobile */
  .content-card {
    padding: 16px;
    border-radius: 12px;
    margin-bottom: 14px;
  }

  .card-title {
    font-size: 16px;
  }

  .card-desc {
    font-size: 13px;
  }

  .card-divider {
    margin: 12px 0 14px;
  }

  /* Card 2: Thông tin ứng viên mobile (1 cột hoàn toàn) */
  .applicant-grid {
    display: flex;
    flex-direction: column;
    gap: 0;
    width: 100%;
  }

  .applicant-field {
    grid-column: auto !important;
    grid-row: auto !important;
    padding: 10px 0;
    border-bottom: 1px solid #eae6dd;
    width: 100%;
    box-sizing: border-box;
  }

  .applicant-field:first-child {
    padding-top: 0;
  }

  .applicant-field:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }

  .field-label {
    font-size: 12px;
    margin-bottom: 2px;
  }

  .field-val {
    font-size: 14px;
    font-weight: 700;
  }

  .letter-box {
    margin-top: 6px;
    padding: 10px 14px;
    font-size: 13.5px;
  }

  /* Card 3: Hồ sơ CV mobile */
  .cv-desktop-layout {
    display: none;
  }

  .cv-mobile-layout {
    display: block;
    width: 100%;
  }

  .cv-mobile-header {
    margin-bottom: 14px;
  }

  .cv-mobile-thumb-row {
    display: flex;
    align-items: center;
    gap: 14px;
    margin-bottom: 16px;
  }

  .cv-mobile-thumb {
    width: 84px;
    height: 116px;
    border: 1px solid #cfcabc;
    border-radius: 6px;
    overflow: hidden;
    position: relative;
    background: #2a2e33;
    cursor: pointer;
    flex-shrink: 0;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
  }

  .cv-thumb-iframe {
    width: 336px;
    height: 464px;
    border: none;
    transform: scale(0.25);
    transform-origin: top left;
    pointer-events: none;
  }

  .cv-thumb-mask {
    position: absolute;
    inset: 0;
    background: transparent;
    cursor: pointer;
  }

  .cv-mobile-meta {
    flex: 1;
    min-width: 0;
  }

  .cv-mobile-actions {
    display: flex;
    flex-direction: column;
    gap: 10px;
    width: 100%;
  }

  .btn-primary-fullscreen {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    background: var(--ui-primary-600, #1f6650);
    color: #ffffff;
    border: none;
    border-radius: 8px;
    padding: 12px 16px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    box-sizing: border-box;
    transition: background 0.15s ease;
  }

  .btn-primary-fullscreen:hover {
    background: var(--ui-primary-700, #19523f);
  }

  .cv-mobile-open-link {
    display: block;
    text-align: center;
    font-size: 13.5px;
    font-weight: 600;
    color: #1c201e;
    text-decoration: none;
    padding: 4px 0;
    transition: color 0.15s ease;
  }

  .cv-mobile-open-link:hover {
    color: var(--ui-primary-600, #1f6650);
  }

  /* Card 4: Xét duyệt đơn mobile */
  .action-card-layout {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }

  .action-aside {
    width: 100%;
  }

  .action-aside .card-desc {
    display: none;
  }

  .action-divider {
    display: none;
  }

  .action-main {
    width: 100%;
    gap: 16px;
  }

  .action-status-info {
    font-size: 13px;
    color: #5f655f;
    line-height: 1.45;
  }

  .action-status-title {
    font-size: 13px;
    font-weight: 600;
    color: #5f655f;
    display: inline;
    margin: 0;
  }

  .action-status-title::after {
    content: '. ';
  }

  .action-status-desc {
    font-size: 13px;
    color: #5f655f;
    display: inline;
    margin: 0;
  }

  .action-buttons-wrap {
    flex-direction: column;
    gap: 10px;
    width: 100%;
  }

  .btn-action-invite,
  .btn-action-reject {
    width: 100%;
    padding: 12px;
    font-size: 14px;
    border-radius: 8px;
  }
}
</style>
