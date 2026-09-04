<script lang="ts" setup>
import { computed } from 'vue'
import { TILES } from '@/data/tiles'
import TileIcon from '@/components/TileIcon.vue'
import UpcomingTasksWidget from '@/components/UpcomingTasksWidget.vue'

const liveCount = computed(() => TILES.filter((t) => t.live).length)
</script>

<template>
  <main class="nb-dashboard">
    <h1 class="nb-dashboard__title">Dashboard</h1>
    <p class="nb-dashboard__subtitle">
      {{ TILES.length }} modules. {{ liveCount }} {{ liveCount === 1 ? 'is' : 'are' }} built. Pick one below.
    </p>
    <UpcomingTasksWidget />
    <div class="nb-dashboard__grid">
      <a
        v-for="tile in TILES.filter((t) => t.live)"
        :key="tile.id"
        class="nb-panel nb-tile nb-tile--live"
        :href="tile.route ?? undefined"
      >
        <span class="nb-chip nb-chip--active nb-tile__tag">LIVE</span>
        <div class="nb-tile__icon-box">
          <TileIcon :name="tile.icon" />
        </div>
        <span class="nb-tile__name">{{ tile.name }}</span>
        <span class="nb-tile__description">{{ tile.description }}</span>
        <span class="nb-tile__footer nb-mono">OPEN &rarr;</span>
      </a>
      <div
        v-for="tile in TILES.filter((t) => !t.live)"
        :key="tile.id"
        class="nb-tile nb-tile--soon"
      >
        <div class="nb-tile__ribbon">
          <div class="nb-tile__ribbon-band">
            <span class="nb-tile__ribbon-text nb-mono">COMING SOON</span>
          </div>
        </div>
        <div class="nb-tile__icon-box nb-tile__icon-box--soon">
          <TileIcon :name="tile.icon" />
        </div>
        <span class="nb-tile__name">{{ tile.name }}</span>
        <span class="nb-tile__description">{{ tile.description }}</span>
        <span class="nb-tile__footer nb-tile__footer--soon nb-mono">NOT YET BUILT</span>
      </div>
    </div>
  </main>
</template>

<style scoped>
.nb-dashboard {
  max-width: 1100px;
  margin: 0 auto;
  padding: var(--nb-space-8) 24px;
}

.nb-dashboard__title {
  margin: 0 0 var(--nb-space-2);
  font-size: 28px;
  font-weight: 700;
  animation: nb-rise-in 320ms ease-out both;
}

.nb-dashboard__subtitle {
  margin: 0 0 var(--nb-space-8);
  font-size: 14px;
  color: var(--nb-color-muted);
  animation: nb-rise-in 340ms 40ms ease-out both;
}

.nb-dashboard__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: var(--nb-space-5);
  animation: nb-rise-in 380ms 120ms ease-out both;
}

.nb-tile {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-3);
  padding: var(--nb-space-5);
  overflow: hidden;
  text-decoration: none;
  color: var(--nb-color-ink);
  animation: nb-rise-in 300ms ease-out both;
  transition:
    transform var(--nb-transition-fast),
    box-shadow var(--nb-transition-fast),
    background-color var(--nb-transition-fast);

  &:nth-child(2n) {
    animation-delay: 35ms;
  }

  &:nth-child(3n) {
    animation-delay: 70ms;
  }

  &:nth-child(4n) {
    animation-delay: 105ms;
  }
}

.nb-tile--live {
  cursor: pointer;

  &:hover {
    transform: translateY(-3px);
    box-shadow: 4px 4px 0 var(--nb-color-ink);
  }
}

.nb-tile--soon {
  border: var(--nb-border-width-lg) dashed var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-muted);
  opacity: 0.85;
  cursor: default;
}

.nb-tile__icon-box {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
}

.nb-tile__icon-box--soon {
  border-style: dashed;
  opacity: 0.5;
}

.nb-tile__name {
  font-size: 16px;
  font-weight: 700;
}

.nb-tile--soon .nb-tile__name {
  color: var(--nb-color-muted);
}

.nb-tile__description {
  font-size: 13px;
  color: var(--nb-color-muted);
}

.nb-tile__footer {
  margin-top: auto;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
  transition: transform var(--nb-transition-fast);

  .nb-tile--live:hover & {
    transform: translateX(3px);
  }
}

.nb-tile__footer--soon {
  color: var(--nb-color-muted);
}


.nb-tile__tag {
  position: absolute;
  top: 0;
  right: 0;
  text-transform: uppercase;
}

/* Hazard-stripe corner ribbon for "coming soon" tiles. */
.nb-tile__ribbon {
  position: absolute;
  top: 0;
  right: 0;
  width: 88px;
  height: 88px;
  overflow: hidden;
  pointer-events: none;
}

.nb-tile__ribbon-band {
  position: absolute;
  top: 14px;
  right: -30px;
  width: 130px;
  padding: 3px 0;
  text-align: center;
  transform: rotate(45deg);
  background: repeating-linear-gradient(
    45deg,
    var(--nb-color-accent-yellow),
    var(--nb-color-accent-yellow) 8px,
    var(--nb-color-ink) 8px,
    var(--nb-color-ink) 16px
  );
  border-top: var(--nb-border-width-md) solid var(--nb-color-ink);
  border-bottom: var(--nb-border-width-md) solid var(--nb-color-ink);
}

.nb-tile__ribbon-text {
  background: var(--nb-color-bg);
  padding: 1px 4px;
  color: var(--nb-color-ink);
  font-size: 9px;
  font-weight: 700;
}
</style>
