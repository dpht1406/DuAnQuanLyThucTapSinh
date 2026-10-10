<script setup>
import { computed } from 'vue'

const props = defineProps({
  label: {
    type: String,
    required: true
  },
  for: {
    type: String,
    default: ''
  },
  error: {
    type: String,
    default: ''
  },
  hint: {
    type: String,
    default: ''
  },
  optional: {
    type: Boolean,
    default: false
  },
  errorId: {
    type: String,
    default: ''
  }
})

const computedErrorId = computed(() => {
  if (props.errorId) return props.errorId
  if (props.for) {
    return props.for.endsWith('-input')
      ? `${props.for.slice(0, -6)}-error`
      : `${props.for}-error`
  }
  return undefined
})
</script>

<template>
  <div class="form-field" :class="{ 'has-error': Boolean(error) }">
    <div class="form-field-header">
      <label v-if="label" :for="props.for" class="form-field-label">
        {{ label }}
        <span v-if="optional" class="form-field-optional">Tùy chọn</span>
      </label>
    </div>

    <div class="form-field-control">
      <slot />
    </div>

    <p v-if="hint && !error" class="form-field-hint">
      {{ hint }}
    </p>

    <p
      v-if="error"
      :id="computedErrorId"
      class="form-field-error"
      role="alert"
      aria-live="polite"
    >
      {{ error }}
    </p>
  </div>
</template>

<style scoped>
.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}

.form-field-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
}

.form-field-label {
  display: inline-flex;
  align-items: baseline;
  gap: 8px;
  font-family: var(--ui-font-sans);
  font-size: 14px;
  font-weight: 500;
  line-height: 1.4;
  color: var(--ui-ink);
}

.form-field-optional {
  font-size: 12px;
  font-weight: 400;
  color: var(--ui-muted);
}

.form-field-control {
  position: relative;
  width: 100%;
}

.form-field-hint {
  margin: 0;
  font-family: var(--ui-font-sans);
  font-size: 12.5px;
  line-height: 1.4;
  color: var(--ui-muted);
}

.form-field-error {
  margin: 0;
  font-family: var(--ui-font-sans);
  font-size: 12.5px;
  line-height: 1.4;
  font-weight: 500;
  color: var(--ui-error);
}
</style>
