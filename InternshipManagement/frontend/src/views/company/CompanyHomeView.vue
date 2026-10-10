<script setup>
import { useAuthStore } from '../../stores/auth.js'

const authStore = useAuthStore()
</script>

<template>
  <div class="page-container">
    <h1 class="page-title">Trang doanh nghiệp</h1>
    <p>Giao diện dành cho doanh nghiệp đang được xây dựng</p>
    <p>{{ authStore.user?.username ?? '' }}</p>
  </div>
</template>
