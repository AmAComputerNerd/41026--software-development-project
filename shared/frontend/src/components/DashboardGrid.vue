<script lang="ts" setup>
import { computed } from 'vue'
import { TILES } from '@/data/tiles'
import TileIcon from '@/components/TileIcon.vue'

const liveCount = computed(() => TILES.filter((t) => t.live).length)
</script>

<template>
  <main class="shell-dashboard">
    <h1 class="shell-dashboard__title">Dashboard</h1>
    <p class="shell-dashboard__subtitle">
      {{ TILES.length }} modules. {{ liveCount }} {{ liveCount === 1 ? 'is' : 'are' }} built. Pick one below.
    </p>
    <div class="shell-grid">
      <a
        v-for="tile in TILES.filter((t) => t.live)"
        :key="tile.id"
        class="shell-tile shell-tile--live"
        :href="tile.route ?? undefined"
      >
        <span class="shell-tile__tag shell-tile__tag--live">LIVE</span>
        <div class="shell-tile__icon-box">
          <TileIcon :name="tile.icon" />
        </div>
        <span class="shell-tile__name">{{ tile.name }}</span>
        <span class="shell-tile__description">{{ tile.description }}</span>
        <span class="shell-tile__footer">OPEN &rarr;</span>
      </a>
      <div
        v-for="tile in TILES.filter((t) => !t.live)"
        :key="tile.id"
        class="shell-tile shell-tile--soon"
      >
        <div class="shell-tile__ribbon">
          <div class="shell-tile__ribbon-band">
            <span class="shell-tile__ribbon-text">COMING SOON</span>
          </div>
        </div>
        <div class="shell-tile__icon-box shell-tile__icon-box--soon">
          <TileIcon :name="tile.icon" />
        </div>
        <span class="shell-tile__name">{{ tile.name }}</span>
        <span class="shell-tile__description">{{ tile.description }}</span>
        <span class="shell-tile__footer shell-tile__footer--soon">NOT YET BUILT</span>
      </div>
    </div>
  </main>
</template>

<style scoped>
.shell-dashboard {
  max-width: 1280px;
  margin: 0 auto;
  padding: 44px 32px 64px;
}

.shell-dashboard__title {
  margin: 0 0 6px;
  font-size: 32px;
  font-weight: 700;
}

.shell-dashboard__subtitle {
  margin: 0 0 var(--space-7);
  font-size: 14px;
  color: var(--color-text-secondary);
}

.shell-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 28px;
}

.shell-tile {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 26px;
  overflow: hidden;
  text-decoration: none;
  color: var(--color-text-primary);
  transition: none;
}

.shell-tile--live {
  border: var(--border-width-lg) solid var(--border-color);
  background: var(--color-surface);
  box-shadow: var(--shadow-offset-md) var(--shadow-offset-md) 0 var(--shadow-color);
  cursor: pointer;
}

.shell-tile--soon {
  border: var(--border-width-md) dashed var(--border-color);
  background: var(--color-background);
  color: var(--color-text-muted);
  opacity: 0.85;
  cursor: default;
}

.shell-tile__icon-box {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--space-4);
  border: var(--border-width-md) solid var(--border-color);
  color: var(--color-text-primary);
}

.shell-tile__icon-box--soon {
  border-style: dashed;
  opacity: 0.5;
}

.shell-tile__name {
  font-size: 20px;
  font-weight: 700;
  margin-bottom: var(--space-2);
}

.shell-tile--soon .shell-tile__name {
  color: var(--color-text-secondary);
}

.shell-tile__description {
  font-size: 13px;
  line-height: 1.5;
  color: var(--color-text-secondary);
}

.shell-tile--live .shell-tile__description {
  color: #333333;
}

.shell-tile--soon .shell-tile__description {
  color: var(--color-text-muted);
}

.shell-tile__footer {
  margin-top: var(--space-5);
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.shell-tile__footer--soon {
  color: var(--color-text-muted);
}

.shell-tile__tag {
  position: absolute;
  top: 0;
  right: 0;
  font-family: var(--font-mono);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  padding: 4px 9px;
  border-left: var(--border-width-md) solid var(--border-color);
  border-bottom: var(--border-width-md) solid var(--border-color);
}

.shell-tile__tag--live {
  background: var(--color-accent-primary);
  color: var(--color-text-primary);
}

/* Hazard-stripe corner ribbon for "coming soon" tiles. */
.shell-tile__ribbon {
  position: absolute;
  top: 0;
  right: 0;
  width: 88px;
  height: 88px;
  overflow: hidden;
  pointer-events: none;
}

.shell-tile__ribbon-band {
  position: absolute;
  top: 14px;
  right: -30px;
  width: 130px;
  padding: 3px 0;
  text-align: center;
  transform: rotate(45deg);
  background: repeating-linear-gradient(
    45deg,
    var(--color-accent-secondary),
    var(--color-accent-secondary) 8px,
    var(--border-color) 8px,
    var(--border-color) 16px
  );
  border-top: var(--border-width-md) solid var(--border-color);
  border-bottom: var(--border-width-md) solid var(--border-color);
}

.shell-tile__ribbon-text {
  background: var(--color-background);
  padding: 1px 4px;
  color: var(--color-text-primary);
  font-family: var(--font-mono);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 0.5px;
}
</style>
