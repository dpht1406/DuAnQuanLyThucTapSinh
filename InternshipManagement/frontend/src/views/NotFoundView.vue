<script setup>
import Card from 'primevue/card'
</script>

<template>
  <div class="page-wrap">
    <Card>
      <template #title>Không tìm thấy trang</template>
      <template #content>Đường dẫn không tồn tại.</template>
    </Card>
  </div>
</template>

<style scoped>
.page-wrap {
  max-width: 32rem;
  margin: 2rem auto;
  padding: 0 1rem;
}
</style>
