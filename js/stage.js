/**
 * Cursus Living Course-Map — WebGL stage
 * Glowing prerequisite constellation + cinematic camera + cascade.
 * Bloom via additive sprite halos (works offline, no EffectComposer).
 * Graph aligned to Mazen / CS211 SVU CS defense narrative.
 */
import * as THREE from '../vendor/three.module.min.js';

const COLORS = {
  primary: 0x4f46e5,
  violet: 0x8b5cf6,
  magenta: 0xec4899,
  done: 0x34d399,
  active: 0x38bdf8,
  fail: 0xf43f5e,
  idle: 0x64748b,
  locked: 0x475569,
  edge: 0x6366f1,
};

function easeOutCubic(t) {
  return 1 - Math.pow(1 - t, 3);
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

function colorToThree(hex) {
  return new THREE.Color(hex);
}

/**
 * Scene clusters map 1:1 to presentation beats (cold → close).
 * Positions: early curriculum left, CS211 cascade center, recovery/platform right.
 */
function buildGraphData() {
  const clusters = {
    cover:         { id: 'cover',         focus: new THREE.Vector3(0, 0.8, 0),   distance: 32 },
    intro:         { id: 'intro',         focus: new THREE.Vector3(-8, 1.2, 0),  distance: 15 },
    architecture:  { id: 'architecture',  focus: new THREE.Vector3(0, 2.5, -2),  distance: 22 },
    impact:        { id: 'impact',        focus: new THREE.Vector3(1, 0.4, 1.5), distance: 12 },
    gpa:           { id: 'gpa',           focus: new THREE.Vector3(-3.5, -1.0, 1.0), distance: 12 },
    progress:      { id: 'progress',      focus: new THREE.Vector3(8, 1.8, 0.5), distance: 14 },
    planner:       { id: 'planner',       focus: new THREE.Vector3(5.5, 2.0, 1.0), distance: 12 },
    advisor:       { id: 'advisor',       focus: new THREE.Vector3(9.5, 0.2, 1.2), distance: 12 },
    admin:         { id: 'admin',         focus: new THREE.Vector3(13.5, 0.8, -1.2), distance: 14 },
    superadmin:    { id: 'superadmin',    focus: new THREE.Vector3(15.5, 2.4, -2.0), distance: 14 },
    challenges:    { id: 'challenges',    focus: new THREE.Vector3(12.0, -1.4, 0.8), distance: 14 },
    close:         { id: 'close',         focus: new THREE.Vector3(2.0, 1.0, 0),   distance: 26 },
  };

  // Mazen / SVU CS constellation — CS211 is the fail seed (Impact Analyzer keystone)
  const nodes = [
    // Early years (completed)
    { id: 'CS121', label: 'CS121', cluster: 'intro', status: 'done',   pos: [-14,  2.0, -1.0] },
    { id: 'CS141', label: 'CS141', cluster: 'intro', status: 'done',   pos: [-12, -0.2,  0.6] },
    { id: 'CS241', label: 'CS241', cluster: 'intro', status: 'done',   pos: [-10,  1.6,  0.2] },
    { id: 'MA111', label: 'MA111', cluster: 'intro', status: 'done',   pos: [-13, -1.8, -0.8] },

    // Year 2 spring — in progress
    { id: 'CS211', label: 'CS211', cluster: 'impact', status: 'active', pos: [-2.5, 1.2, 0.4], failSeed: true, anchor: true },
    { id: 'MA222', label: 'MA222', cluster: 'gpa',    status: 'active', pos: [-5.5, -0.8, 1.8], anchor: true },
    { id: 'IS211', label: 'IS211', cluster: 'gpa',    status: 'active', pos: [-3.5, -1.6, -0.6] },
    { id: 'CS242', label: 'CS242', cluster: 'gpa',    status: 'active', pos: [-1.0, -1.2, 2.0] },

    // Downstream cascade (blocked if CS211 fails) — locked until heal
    { id: 'CS311', label: 'CS311', cluster: 'impact', status: 'locked', pos: [ 1.5,  2.2,  0.2] },
    { id: 'CS312', label: 'CS312', cluster: 'impact', status: 'locked', pos: [ 2.0,  0.0,  1.8] },
    { id: 'CS331', label: 'CS331', cluster: 'impact', status: 'locked', pos: [ 3.5,  1.4, -0.8] },
    { id: 'AI301', label: 'AI301', cluster: 'impact', status: 'locked', pos: [ 4.5, -0.6,  1.2] },
    { id: 'IS313', label: 'IS313', cluster: 'planner', status: 'locked', pos: [ 5.5,  2.0,  1.0] },

    // Recovery / graduation path
    { id: 'CS411', label: 'CS411', cluster: 'progress', status: 'idle', pos: [ 8.0,  1.8, -0.4], anchor: true },
    { id: 'CS451', label: 'CS451', cluster: 'advisor',  status: 'idle', pos: [ 9.5,  0.2,  1.2], anchor: true },
    { id: 'CS492', label: 'CS492', cluster: 'close',    status: 'idle', pos: [11.0,  1.4,  0.4], anchor: true },

    // Institutional layer (Admin / Super Admin)
    { id: 'ADMIN', label: 'ADMIN', cluster: 'admin',      status: 'idle', pos: [13.5,  0.8, -1.2], anchor: true },
    { id: 'PLAT',  label: 'PLAT',  cluster: 'superadmin', status: 'idle', pos: [15.5,  2.4, -2.0], anchor: true },
    { id: 'DATA',  label: 'DATA',  cluster: 'challenges', status: 'idle', pos: [12.0, -1.4,  0.8], anchor: true },

    // Ambient bridges
    { id: 'UNI101', label: 'UNI', cluster: 'architecture', status: 'done', pos: [-6,  3.6, -3.2], anchor: true },
    { id: 'SVU',    label: 'SVU', cluster: 'architecture', status: 'done', pos: [ 2,  3.8, -2.8] },
  ];

  const edges = [
    ['CS121', 'CS141'],
    ['CS141', 'CS241'],
    ['CS241', 'CS211'],
    ['MA111', 'CS211'],
    ['CS211', 'CS311'],
    ['CS211', 'CS312'],
    ['CS211', 'CS331'],
    ['CS211', 'AI301'],
    ['CS211', 'IS313'],
    ['CS311', 'CS411'],
    ['CS312', 'CS411'],
    ['CS331', 'CS451'],
    ['AI301', 'CS451'],
    ['CS411', 'CS492'],
    ['CS451', 'CS492'],
    ['IS313', 'CS492'],
    ['MA222', 'CS211'],
    ['IS211', 'IS313'],
    ['CS242', 'CS311'],
    ['UNI101', 'CS121'],
    ['SVU', 'ADMIN'],
    ['ADMIN', 'PLAT'],
    ['PLAT', 'DATA'],
    ['DATA', 'CS492'],
  ];

  return { clusters, nodes, edges };
}

function statusColor(status) {
  switch (status) {
    case 'done':   return COLORS.done;
    case 'active': return COLORS.active;
    case 'fail':   return COLORS.fail;
    case 'locked': return COLORS.locked;
    default:       return COLORS.violet;
  }
}

export class GraphStage {
  /**
   * @param {HTMLElement} mount
   */
  constructor(mount) {
    this.mount = mount;
    this.data = buildGraphData();
    this.nodeMeshes = new Map();
    this.nodeGlows = new Map();
    this.edgeLines = [];
    this.edgePulseUniforms = [];
    this.clock = new THREE.Clock();
    this.sceneId = 'cover';
    this.cameraFlight = null;
    this.cascade = null;
    this.breathPhase = 0;
    this._raf = 0;

    // Raycasting for interactive hover tooltips
    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2(-9999, -9999); // offscreen initially
    this.hoveredNodeId = null;

    this._initRenderer();
    this._initScene();
    this._initCamera();
    this._buildStarfield();
    this._buildGraph();
    this._bindResize();
    this._bindMouseEvents();
    this.flyTo('cover', 0);
    this._animate();
  }

  _initRenderer() {
    this.renderer = new THREE.WebGLRenderer({
      antialias: true,
      alpha: true,
      powerPreference: 'high-performance',
    });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    this.renderer.setSize(this.mount.clientWidth, this.mount.clientHeight);
    this.renderer.setClearColor(0x0a0a0f, 1);
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.mount.appendChild(this.renderer.domElement);
  }

  _initScene() {
    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.FogExp2(0x0a0a0f, 0.018);

    const ambient = new THREE.AmbientLight(0x334155, 0.55);
    this.scene.add(ambient);

    const key = new THREE.PointLight(0x818cf8, 2.2, 80);
    key.position.set(8, 12, 10);
    this.scene.add(key);

    const rim = new THREE.PointLight(0xec4899, 1.1, 60);
    rim.position.set(-12, 4, -8);
    this.scene.add(rim);
  }

  _initCamera() {
    const aspect = this.mount.clientWidth / Math.max(this.mount.clientHeight, 1);
    this.camera = new THREE.PerspectiveCamera(42, aspect, 0.1, 200);
    this.camera.position.set(0, 6, 32);
    this.cameraTarget = new THREE.Vector3(0, 0.8, 0);
    this.camera.lookAt(this.cameraTarget);
  }

  _buildStarfield() {
    const count = 900;
    const positions = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      positions[i * 3]     = (Math.random() - 0.5) * 120;
      positions[i * 3 + 1] = (Math.random() - 0.5) * 80;
      positions[i * 3 + 2] = (Math.random() - 0.5) * 100 - 20;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    const mat = new THREE.PointsMaterial({
      color: 0xc4b5fd,
      size: 0.08,
      transparent: true,
      opacity: 0.7,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
    });
    this.stars = new THREE.Points(geo, mat);
    this.scene.add(this.stars);
  }

  _makeGlowTexture() {
    const size = 128;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext('2d');
    const g = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
    g.addColorStop(0, 'rgba(255,255,255,1)');
    g.addColorStop(0.25, 'rgba(255,255,255,0.55)');
    g.addColorStop(0.55, 'rgba(255,255,255,0.12)');
    g.addColorStop(1, 'rgba(255,255,255,0)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, size, size);
    const tex = new THREE.CanvasTexture(canvas);
    tex.colorSpace = THREE.SRGBColorSpace;
    return tex;
  }

  _buildGraph() {
    this.glowTexture = this._makeGlowTexture();
    const group = new THREE.Group();
    this.graphGroup = group;
    this.scene.add(group);

    for (const node of this.data.nodes) {
      const color = statusColor(node.status);
      const core = new THREE.Mesh(
        new THREE.SphereGeometry(0.28, 32, 32),
        new THREE.MeshBasicMaterial({ color }),
      );
      core.position.set(...node.pos);
      core.userData = { id: node.id, baseColor: color, status: node.status, failSeed: !!node.failSeed };
      group.add(core);
      this.nodeMeshes.set(node.id, core);

      const glowMat = new THREE.SpriteMaterial({
        map: this.glowTexture,
        color,
        transparent: true,
        opacity: node.status === 'locked' ? 0.4 : 0.95,
        blending: THREE.AdditiveBlending,
        depthWrite: false,
      });
      const glow = new THREE.Sprite(glowMat);
      const glowSize = node.status === 'locked' ? 2.1 : node.status === 'done' ? 2.8 : 3.1;
      glow.scale.set(glowSize, glowSize, 1);
      glow.userData.baseScale = glowSize;
      glow.position.copy(core.position);
      group.add(glow);
      this.nodeGlows.set(node.id, glow);
    }

    const edgeVert = /* glsl */`
      attribute float lineProgress;
      varying float vProgress;
      void main() {
        vProgress = lineProgress;
        gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
      }
    `;
    const edgeFrag = /* glsl */`
      uniform vec3 uColor;
      uniform float uTime;
      uniform float uIntensity;
      uniform float uFailMix;
      varying float vProgress;
      void main() {
        float pulse = smoothstep(0.0, 0.12, abs(fract(vProgress - uTime * 0.18) - 0.5));
        pulse = 1.0 - pulse;
        vec3 failCol = vec3(0.957, 0.247, 0.369);
        vec3 col = mix(uColor, failCol, uFailMix);
        float alpha = (0.35 + pulse * 0.9 * uIntensity) * (0.7 + uFailMix * 0.35);
        gl_FragColor = vec4(col * (1.0 + pulse * 0.55), alpha);
      }
    `;

    for (const [aId, bId] of this.data.edges) {
      const a = this.nodeMeshes.get(aId);
      const b = this.nodeMeshes.get(bId);
      if (!a || !b) continue;

      const positions = new Float32Array([
        a.position.x, a.position.y, a.position.z,
        b.position.x, b.position.y, b.position.z,
      ]);
      const progress = new Float32Array([0, 1]);
      const geo = new THREE.BufferGeometry();
      geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
      geo.setAttribute('lineProgress', new THREE.BufferAttribute(progress, 1));

      const uniforms = {
        uColor: { value: colorToThree(COLORS.violet) },
        uTime: { value: 0 },
        uIntensity: { value: 1.15 },
        uFailMix: { value: 0 },
      };
      this.edgePulseUniforms.push(uniforms);

      const mat = new THREE.ShaderMaterial({
        uniforms,
        vertexShader: edgeVert,
        fragmentShader: edgeFrag,
        transparent: true,
        depthWrite: false,
        blending: THREE.AdditiveBlending,
      });

      const line = new THREE.Line(geo, mat);
      line.userData = { from: aId, to: bId };
      group.add(line);
      this.edgeLines.push(line);
    }

    this.adj = new Map();
    for (const [a, b] of this.data.edges) {
      if (!this.adj.has(a)) this.adj.set(a, []);
      this.adj.get(a).push(b);
    }
  }

  getNodeScreenPos(nodeId) {
    const mesh = this.nodeMeshes.get(nodeId);
    if (!mesh) return null;
    const v = mesh.position.clone().project(this.camera);
    const w = this.mount.clientWidth;
    const h = this.mount.clientHeight;
    return {
      x: (v.x * 0.5 + 0.5) * w,
      y: (-v.y * 0.5 + 0.5) * h,
      visible: v.z < 1,
    };
  }

  getClusterAnchor(clusterId) {
    const node = this.data.nodes.find((n) => n.cluster === clusterId && n.anchor)
      || this.data.nodes.find((n) => n.cluster === clusterId);
    return node ? node.id : null;
  }

  getFailSeedId() {
    const n = this.data.nodes.find((x) => x.failSeed);
    return n ? n.id : 'CS211';
  }

  flyTo(sceneId, duration = 1.15) {
    const cluster = this.data.clusters[sceneId];
    if (!cluster) return;

    this.sceneId = sceneId;
    const focus = cluster.focus.clone();
    const dist = cluster.distance;

    const endPos = new THREE.Vector3(
      focus.x + dist * 0.15,
      focus.y + dist * 0.28,
      focus.z + dist * 0.92,
    );

    if (duration <= 0) {
      this.camera.position.copy(endPos);
      this.cameraTarget.copy(focus);
      this.camera.lookAt(this.cameraTarget);
      this.cameraFlight = null;
      return;
    }

    this.cameraFlight = {
      t: 0,
      duration,
      fromPos: this.camera.position.clone(),
      toPos: endPos,
      fromTarget: this.cameraTarget.clone(),
      toTarget: focus,
    };
  }

  /**
   * Signature cascade: fail seed → ripple downstream → desaturate → heal.
   * @param {{ onDesat?: Function, onHeal?: Function, onDone?: Function }} hooks
   */
  playCascade(hooks = {}) {
    if (this.cascade?.active) return;
    const seed = this.getFailSeedId();
    const order = this._bfs(seed);
    this.cascade = {
      active: true,
      phase: 'ignite',
      t: 0,
      seed,
      order,
      rippled: new Set(),
      hooks,
    };
  }

  _bfs(start) {
    const out = [];
    const seen = new Set([start]);
    const q = [start];
    while (q.length) {
      const id = q.shift();
      out.push(id);
      for (const next of this.adj.get(id) || []) {
        if (!seen.has(next)) {
          seen.add(next);
          q.push(next);
        }
      }
    }
    return out;
  }

  _setNodeColor(id, hex, glowScale = 1) {
    const mesh = this.nodeMeshes.get(id);
    const glow = this.nodeGlows.get(id);
    if (mesh) {
      mesh.material.color.setHex(hex);
    }
    if (glow) {
      glow.material.color.setHex(hex);
      glow.material.opacity = 0.98;
      const base = glow.userData.baseScale || 2.6;
      glow.scale.setScalar(base * glowScale);
    }
  }

  _resetNodeLooks() {
    for (const node of this.data.nodes) {
      const c = statusColor(node.status);
      this._setNodeColor(node.id, c, 1);
      const glow = this.nodeGlows.get(node.id);
      if (glow) glow.material.opacity = node.status === 'locked' ? 0.4 : 0.95;
    }
    for (const u of this.edgePulseUniforms) {
      u.uFailMix.value = 0;
      u.uIntensity.value = 1.15;
    }
  }

  /** Soft heal without full cascade (planner / close beats). */
  playHeal(hooks = {}) {
    this._resetNodeLooks();
    // Re-ignite locked downstream as recovered (violet → done tint)
    const recovered = ['CS311', 'CS312', 'CS331', 'AI301', 'IS313', 'CS411', 'CS451', 'CS492'];
    for (const id of recovered) {
      this._setNodeColor(id, COLORS.violet, 1.25);
    }
    this._setNodeColor('CS211', COLORS.done, 1.35);
    hooks.onDone?.();
  }

  _updateCascade(dt) {
    const c = this.cascade;
    if (!c?.active) return;
    c.t += dt;

    if (c.phase === 'ignite') {
      this._setNodeColor(c.seed, COLORS.fail, 1.8);
      if (c.t > 0.45) {
        c.phase = 'ripple';
        c.t = 0;
        c.rippleIndex = 1;
      }
    } else if (c.phase === 'ripple') {
      const interval = 0.22;
      while (c.t >= interval && c.rippleIndex < c.order.length) {
        const id = c.order[c.rippleIndex];
        this._setNodeColor(id, COLORS.fail, 1.35);
        c.rippled.add(id);
        for (const line of this.edgeLines) {
          if (line.userData.to === id || line.userData.from === c.order[c.rippleIndex - 1]) {
            line.material.uniforms.uFailMix.value = 1;
            line.material.uniforms.uIntensity.value = 1.6;
          }
        }
        c.rippleIndex++;
        c.t -= interval;
      }
      if (c.rippleIndex >= c.order.length) {
        c.phase = 'stakes';
        c.t = 0;
        c.hooks.onDesat?.();
      }
    } else if (c.phase === 'stakes') {
      if (c.t > 0.85) {
        c.phase = 'heal';
        c.t = 0;
        c.hooks.onHeal?.();
      }
    } else if (c.phase === 'heal') {
      const progress = Math.min(1, c.t / 1.4);
      for (let i = 0; i < c.order.length; i++) {
        const local = Math.min(1, Math.max(0, (progress * c.order.length - i) / 1.5));
        if (local <= 0) continue;
        const id = c.order[i];
        const node = this.data.nodes.find((n) => n.id === id);
        const base = statusColor(node?.status || 'idle');
        const mesh = this.nodeMeshes.get(id);
        if (mesh) {
          mesh.material.color.lerpColors(colorToThree(COLORS.fail), colorToThree(base), local);
        }
        const glow = this.nodeGlows.get(id);
        if (glow) {
          glow.material.color.copy(mesh.material.color);
          glow.scale.setScalar(lerp(1.35 * 2.4, 2.4, local));
        }
      }
      for (const u of this.edgePulseUniforms) {
        u.uFailMix.value = 1 - progress;
      }
      if (progress >= 1) {
        c.phase = 'done';
        this._resetNodeLooks();
        c.active = false;
        c.hooks.onDone?.();
        this.cascade = null;
      }
    }
  }

  _bindResize() {
    this._onResize = () => {
      const w = this.mount.clientWidth;
      const h = this.mount.clientHeight;
      this.camera.aspect = w / Math.max(h, 1);
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(w, h);
    };
    window.addEventListener('resize', this._onResize);
  }

  _bindMouseEvents() {
    this._onMouseMove = (e) => {
      const rect = this.renderer.domElement.getBoundingClientRect();
      this.mouse.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
      this.mouse.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    };
    window.addEventListener('mousemove', this._onMouseMove);
  }

  _animate = () => {
    this._raf = requestAnimationFrame(this._animate);
    const dt = Math.min(this.clock.getDelta(), 0.05);
    const t = this.clock.elapsedTime;
    this.breathPhase = t;

    if (this.graphGroup) {
      this.graphGroup.rotation.y = Math.sin(t * 0.12) * 0.04;
      this.graphGroup.position.y = Math.sin(t * 0.35) * 0.12;
    }
    if (this.stars) {
      this.stars.rotation.y = t * 0.01;
    }

    for (const [id, glow] of this.nodeGlows) {
      if (this.cascade?.active) continue;
      const base = glow.userData.baseScale || 2.6;
      const pulse = 1 + Math.sin(t * 1.6 + id.length) * 0.07;
      glow.scale.setScalar(base * pulse);
    }

    for (const u of this.edgePulseUniforms) {
      u.uTime.value = t;
    }

    if (this.cameraFlight) {
      const f = this.cameraFlight;
      f.t += dt;
      const p = easeOutCubic(Math.min(1, f.t / f.duration));
      this.camera.position.lerpVectors(f.fromPos, f.toPos, p);
      this.cameraTarget.lerpVectors(f.fromTarget, f.toTarget, p);
      this.camera.lookAt(this.cameraTarget);
      if (p >= 1) this.cameraFlight = null;
    } else {
      this.camera.position.x += Math.sin(t * 0.25) * 0.002;
      this.camera.lookAt(this.cameraTarget);
    }

    // Raycasting for interactive hover tooltips in 3D graph space
    if (this.sceneId !== 'cover' && !this.cascade?.active) {
      this.raycaster.setFromCamera(this.mouse, this.camera);
      const intersects = this.raycaster.intersectObjects(Array.from(this.nodeMeshes.values()));
      const tooltip = document.getElementById('node-tooltip');

      if (intersects.length > 0) {
        const hit = intersects[0].object;
        const id = hit.userData.id;
        const node = this.data.nodes.find(n => n.id === id);

        if (node && this.hoveredNodeId !== id) {
          // Restore previous hovered node scale
          if (this.hoveredNodeId) {
            const prev = this.nodeMeshes.get(this.hoveredNodeId);
            if (prev) prev.scale.setScalar(1);
          }

          this.hoveredNodeId = id;
          hit.scale.setScalar(1.4); // scale up node sphere on hover

          if (tooltip) {
            const titleEl = tooltip.querySelector('.tooltip-title');
            const statusEl = tooltip.querySelector('.tooltip-status');
            const metaEl = tooltip.querySelector('.tooltip-meta');

            if (titleEl) titleEl.textContent = node.id;
            if (statusEl) {
              statusEl.textContent = node.status;
              statusEl.className = 'tooltip-status ' + node.status;
            }
            if (metaEl) {
              const details = {
                CS121: 'Credits: 3 · Intro to Computer Science',
                CS141: 'Credits: 4 · Structured Programming',
                CS241: 'Credits: 4 · Object-Oriented Programming',
                MA111: 'Credits: 3 · Mathematical Analysis I',
                CS211: 'Credits: 4 · Data Structures & Algorithms (Keystone)',
                MA222: 'Credits: 3 · Probability & Statistics II',
                IS211: 'Credits: 3 · Database Management Systems',
                CS242: 'Credits: 3 · Logic Design & Organization',
                CS311: 'Credits: 3 · Algorithms & Complexity I',
                CS312: 'Credits: 3 · Algorithms & Complexity II',
                CS331: 'Credits: 3 · Visual Programming',
                AI301: 'Credits: 3 · Artificial Intelligence Basics',
                IS313: 'Credits: 3 · File Management & Processing',
                CS411: 'Credits: 3 · Software Engineering Methodology',
                CS451: 'Credits: 3 · Distributed Systems & Cloud',
                CS492: 'Credits: 4 · Graduation Project II',
                ADMIN: 'Sinai University Admin System Scope',
                PLAT:  'Multi-tenant Provisioning Core',
                DATA:  'Sinai Curriculum Database catalog',
                UNI101:'Credits: 2 · English Communication',
                SVU:   'SVU Integration Gateway'
              };
              metaEl.textContent = details[node.id] || 'Credits: 3 · Curriculum Core Course';
            }
            tooltip.classList.add('is-visible');
          }
        }

        // Sync tooltip position to screen coordinates of the hovered node
        if (tooltip && this.hoveredNodeId) {
          const screen = this.getNodeScreenPos(this.hoveredNodeId);
          if (screen && screen.visible) {
            tooltip.style.left = `${screen.x}px`;
            tooltip.style.top = `${screen.y}px`;
          }
        }
      } else {
        if (this.hoveredNodeId) {
          const prev = this.nodeMeshes.get(this.hoveredNodeId);
          if (prev) prev.scale.setScalar(1);
          this.hoveredNodeId = null;
        }
        if (tooltip) {
          tooltip.classList.remove('is-visible');
        }
      }
    } else {
      const tooltip = document.getElementById('node-tooltip');
      if (tooltip) tooltip.classList.remove('is-visible');
    }

    this._updateCascade(dt);
    this.renderer.render(this.scene, this.camera);
    this.onFrame?.(dt);
  };

  dispose() {
    cancelAnimationFrame(this._raf);
    window.removeEventListener('resize', this._onResize);
    window.removeEventListener('mousemove', this._onMouseMove);
    this.renderer.dispose();
  }
}
