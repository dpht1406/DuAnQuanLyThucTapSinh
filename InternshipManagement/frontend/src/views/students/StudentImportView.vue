<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import FileUpload from 'primevue/fileupload'
import Message from 'primevue/message'
import { importStudents } from '../../api/students.js'
import FormActionBar from '../../components/form/FormActionBar.vue'
import FormField from '../../components/form/FormField.vue'
import FormPage from '../../components/form/FormPage.vue'
import FormSection from '../../components/form/FormSection.vue'
import { extractErrorMessage } from '../../utils/apiError.js'

const router = useRouter()
const selectedFile = ref(null)
const fileUpload = ref(null)
const importing = ref(false)
const importError = ref('')
const importResult = ref(null)

function onFileSelect(event) {
  selectedFile.value = event.files?.[0] ?? null
  importError.value = ''
  importResult.value = null
}

async function uploadStudents() {
  if (!selectedFile.value) return

  importing.value = true
  importError.value = ''
  importResult.value = null

  try {
    importResult.value = await importStudents(selectedFile.value)
    selectedFile.value = null
    fileUpload.value?.clear()
  } catch (err) {
    importError.value = extractErrorMessage(err, 'Không thể import sinh viên, vui lòng thử lại.')
  } finally {
    importing.value = false
  }
}
</script>

<template>
  <FormPage
    title="Nhập sinh viên từ file"
    back-label="Quay lại danh sách"
    :back-to="{ name: 'students' }"
    class="student-import-page"
  >
    <FormSection title="Tệp sinh viên">
      <FormField label="Tệp">
        <div class="import-file-control">
          <FileUpload
            ref="fileUpload"
            mode="basic"
            accept=".xlsx,.xls,.csv"
            :auto="false"
            choose-label="Chọn file"
            :disabled="importing"
            @select="onFileSelect"
          />
          <span v-if="selectedFile" class="selected-file">{{ selectedFile.name }}</span>
        </div>
      </FormField>

      <Message v-if="importError" severity="error" :closable="false">
        {{ importError }}
      </Message>
      <Message v-if="importResult" severity="success" :closable="false">
        Đã import thành công {{ importResult.successCount }} sinh viên.
      </Message>
      <Message v-if="importResult?.errors?.length" severity="warn" :closable="false">
        Các dòng lỗi dưới đây đã được bỏ qua.
      </Message>
      <DataTable
        v-if="importResult?.errors?.length"
        :value="importResult.errors"
        size="small"
        class="errors-table"
      >
        <Column field="rowNumber" header="Dòng" />
        <Column field="reason" header="Lý do" />
      </DataTable>
    </FormSection>

    <FormActionBar>
      <Button
        v-if="importResult?.successCount > 0"
        label="Về danh sách sinh viên"
        severity="secondary"
        :disabled="importing"
        @click="router.push({ name: 'students' })"
      />
      <Button
        label="Tải lên"
        icon="pi pi-upload"
        class="btn-primary"
        :loading="importing"
        :disabled="!selectedFile"
        @click="uploadStudents"
      />
    </FormActionBar>
  </FormPage>
</template>

<style scoped>
.import-file-control {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.selected-file {
  color: var(--ui-muted);
  overflow-wrap: anywhere;
}

.errors-table {
  margin-top: 0.5rem;
}

@media (max-width: 640px) {
  .student-import-page {
    min-height: 100dvh;
  }
}
</style>
