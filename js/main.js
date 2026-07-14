/**
 * Presentation orchestrator — single linear deck (outline v2, beats 0–11).
 * Mazen's emotional arc is woven into these beats — no separate story mode.
 */
import { GraphStage } from './stage.js';
import { PanelDirector } from './panels.js';

/** Beats match presentation_outline_order (v2). */
const SCENES = [
  'cover',
  'idea',
  'wireframe',
  'users',
  'datastructure',
  'techstack',
  'liveapp',
  'deliverables',
  'team',
];

/**
 * Mazen avatar moods along the outline spine:
 * cover (dormant) → idea (worried) → wireframe (focused) → rest (off)
 */
const AVATAR_MOOD = {
  cover: 'dormant',
  idea: 'worried',
  wireframe: 'focused',
  users: 'off',
  datastructure: 'off',
  techstack: 'off',
  liveapp: 'off',
  deliverables: 'off',
  team: 'off',
};

function $(sel) {
  return document.querySelector(sel);
}

function setAvatarMood(mood) {
  const el = $('#avatar');
  if (!el) return;
  el.dataset.mood = mood || 'calm';
  const label = el.querySelector('.avatar-mood-label');
  if (label) {
    const labels = {
      dormant: '…',
      calm: 'Calm',
      neutral: 'Listening',
      worried: 'Worried',
      panic: 'Panic',
      focused: 'Focused',
      reassured: 'Reassured',
      confident: 'Confident',
      steady: 'Steady',
      off: '—',
      proud: 'Proud',
    };
    label.textContent = labels[mood] || mood;
  }
  el.classList.toggle('is-hidden', mood === 'off' || mood === 'dormant');
}

function boot() {
  const mount = $('#stage');
  const panelsRoot = $('#panels');
  const bootEl = $('#boot');

  const stage = new GraphStage(mount);
  const panels = new PanelDirector(panelsRoot, stage);

  let sceneIndex = 0;
  let cascading = false;

  function goTo(index, { withCascade = false } = {}) {
    sceneIndex = ((index % SCENES.length) + SCENES.length) % SCENES.length;
    const id = SCENES[sceneIndex];

    const beatEl = $('#beat-label');
    if (beatEl) {
      beatEl.textContent = `${sceneIndex} / ${SCENES.length - 1} · ${id}`;
    }

    // Sync timeline scrubber position
    const scrubber = $('#timeline-scrubber');
    if (scrubber) {
      scrubber.value = sceneIndex;
    }

    // Stop previous code highlighting if navigating away from datastructure
    if (id !== 'datastructure') {
      panels.stopCodeHighlight();
    }

    stage.flyTo(id);
    if (id === 'cover') {
      panels.hideAll();
    } else {
      panels.show(id, { hot: withCascade });
    }
    setAvatarMood(AVATAR_MOOD[id] || 'calm');

    if (id === 'liveapp' || id === 'team') {
      stage.playHeal();
    }
  }

  function next() {
    if (cascading) return;
    goTo(sceneIndex + 1);
  }

  function prev() {
    if (cascading) return;
    goTo(sceneIndex - 1);
  }

  function runCascade() {
    if (cascading) return;
    cascading = true;

    sceneIndex = SCENES.indexOf('liveapp');
    goTo(sceneIndex, { withCascade: true });

    setTimeout(() => {
      stage.playCascade({
        onDesat: () => {
          document.body.classList.add('cascade-desat');
          panels.setHot(true);
        },
        onHeal: () => {
          document.body.classList.remove('cascade-desat');
        },
        onDone: () => {
          cascading = false;
          panels.setHot(false);
        },
      });
    }, 500);
  }

  // Hook up timeline scrubber events
  const scrubber = $('#timeline-scrubber');
  if (scrubber) {
    scrubber.addEventListener('input', () => {
      if (cascading) return;
      goTo(parseInt(scrubber.value));
    });
  }

  // Bind Cascade button
  const cascadeBtn = $('.cascade-btn');
  if (cascadeBtn) {
    cascadeBtn.addEventListener('click', runCascade);
  }

  window.addEventListener('keydown', (e) => {
    if (e.target.matches('input, textarea, select')) return; // Avoid key capture in selects
    switch (e.key) {
      case 'ArrowRight':
      case ' ':
        e.preventDefault();
        next();
        break;
      case 'ArrowLeft':
        e.preventDefault();
        prev();
        break;
      case 'c':
      case 'C':
        e.preventDefault();
        runCascade();
        break;
      case 'h':
      case 'H':
        e.preventDefault();
        stage.playHeal();
        setAvatarMood('confident');
        break;
      default:
        break;
    }
  });

  stage.onFrame = () => {
    panels.sync();
  };

  goTo(0);

  requestAnimationFrame(() => {
    bootEl?.classList.add('is-done');
  });

  window.__cursusStage = { stage, panels, goTo, runCascade, SCENES };
}

boot();
