(function initGraph() {
  const svg = document.getElementById('graph-svg');
  if (!svg) return;

  const nodes = [
    { sel: '.node-1', home: { x: 140, y: 180 }, amp: 14, px: 0.0,  py: 1.3,  sp: 0.00070 },
    { sel: '.node-2', home: { x: 300, y: 120 }, amp: 18, px: 2.1,  py: 0.5,  sp: 0.00055 },
    { sel: '.node-3', home: { x: 480, y: 220 }, amp: 12, px: 4.3,  py: 2.8,  sp: 0.00080 },
    { sel: '.node-4', home: { x: 240, y: 380 }, amp: 16, px: 1.0,  py: 3.7,  sp: 0.00045 },
    { sel: '.node-5', home: { x: 560, y: 460 }, amp: 13, px: 3.5,  py: 0.9,  sp: 0.00065 },
    { sel: '.node-6', home: { x: 400, y: 560 }, amp: 15, px: 5.2,  py: 4.1,  sp: 0.00050 },
    { sel: '.node-7', home: { x: 300, y: 680 }, amp: 11, px: 0.8,  py: 2.3,  sp: 0.00075 },
    { sel: '.node-8', home: { x:  80, y: 500 }, amp:  8, px: 6.0,  py: 1.7,  sp: 0.00060 },
    { sel: '.node-9', home: { x: 620, y: 150 }, amp:  9, px: 2.9,  py: 5.0,  sp: 0.00085 },
  ];

  nodes.forEach(n => { n.el = n.sel ? svg.querySelector(n.sel) : null; });

  const lineMap = {
    c1:  [0, 1], c2:  [1, 2], c3:  [1, 3], c4:  [2, 4],
    c5:  [3, 5], c6:  [4, 5], c7:  [0, 3], c8:  [5, 6],
    c9:  [7, 3], c10: [8, 2],
  };

  const lines = {};
  Object.keys(lineMap).forEach(id => { lines[id] = document.getElementById(id); });

  function tick(t) {
    const pos = nodes.map(n => ({
      x: n.home.x + Math.sin(t * n.sp        + n.px) * n.amp,
      y: n.home.y + Math.cos(t * n.sp * 0.83 + n.py) * n.amp,
    }));

    nodes.forEach((n, i) => {
      if (!n.el) return;
      const dx = pos[i].x - n.home.x;
      const dy = pos[i].y - n.home.y;
      n.el.setAttribute('transform', `translate(${dx.toFixed(2)},${dy.toFixed(2)})`);
    });

    Object.entries(lineMap).forEach(([id, [a, b]]) => {
      const line = lines[id];
      if (!line) return;
      line.setAttribute('x1', pos[a].x.toFixed(2));
      line.setAttribute('y1', pos[a].y.toFixed(2));
      line.setAttribute('x2', pos[b].x.toFixed(2));
      line.setAttribute('y2', pos[b].y.toFixed(2));
    });

    requestAnimationFrame(tick);
  }

  requestAnimationFrame(tick);
})();