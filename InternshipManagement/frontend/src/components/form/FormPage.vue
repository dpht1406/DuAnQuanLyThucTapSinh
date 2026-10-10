<script setup>
import { useRouter } from 'vue-router'

const props = defineProps({
  title: {
    type: String,
    required: true
  },
  subtitle: {
    type: String,
    default: ''
  },
  backLabel: {
    type: String,
    default: ''
  },
  backTo: {
    type: [String, Object],
    default: null
  }
})

const emit = defineEmits(['back'])
const router = useRouter()

function handleBack() {
  emit('back')
  if (props.backTo && router) {
    router.push(props.backTo)
  } else if (router && window.history.length > 1) {
    router.back()
  }
}
</script>

<template>
  <div class="form-page">
    <div class="form-page-container">
      <header class="form-page-header">
        <button
          v-if="backLabel || backTo"
          type="button"
          class="form-back-button"
          @click="handleBack"
        >
          <span aria-hidden="true">&larr;</span>
          <span>{{ backLabel || 'Quay lại' }}</span>
        </button>

        <div class="form-page-headings">
          <h1 class="form-page-title">{{ title }}</h1>
          <p v-if="subtitle" class="form-page-subtitle">{{ subtitle }}</p>
        </div>
      </header>

      <main class="form-page-surface">
        <slot />
        <slot name="actions" />
      </main>
    </div>
  </div>
</template>

<style scoped>
.form-page {
  width: 100%;
  min-height: 100%;
  background: var(--ui-canvas);
}

.form-page-container {
  box-sizing: border-box;
  width: 100%;
  max-width: 920px;
  margin: 0 auto;
  padding: 2.5rem 1.5rem 4rem;
}

.form-page-header {
  margin-bottom: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.form-back-button {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  align-self: flex-start;
  padding: 4px 0;
  border: none;
  background: transparent;
  color: var(--ui-muted);
  font-family: var(--ui-font-sans);
  font-size: 13.5px;
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
  transition: color 120ms ease;
}

.form-back-button:hover {
  color: var(--ui-ink);
}

.form-back-button:focus-visible {
  outline: 2px solid var(--ui-primary-600);
  outline-offset: 2px;
  border-radius: 4px;
}

.form-page-headings {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.form-page-title {
  margin: 0;
  font-family: var(--ui-font-serif);
  font-size: 30px;
  font-weight: 600;
  line-height: 1.25;
  letter-spacing: -0.01em;
  color: var(--ui-ink);
}

.form-page-subtitle {
  margin: 0;
  font-family: var(--ui-font-sans);
  font-size: 14px;
  line-height: 1.5;
  color: var(--ui-muted);
}

.form-page-surface {
  background: var(--ui-surface);
  border: 1px solid var(--ui-border);
  border-radius: var(--ui-radius-md);
  box-shadow: none;
  padding: 2rem 2.5rem;
}

@media (max-width: 640px) {
  .form-page-container {
    padding: 1.25rem 1rem 0;
  }

  .form-page-header {
    margin-bottom: 1.25rem;
    gap: 0.5rem;
  }

  .form-page-title {
    font-size: 24px;
  }

  .form-page-subtitle {
    font-size: 13px;
  }

  .form-page-surface {
    background: transparent;
    border: none;
    border-radius: 0;
    padding: 0;
    box-shadow: none;
  }
}
</style>
