// FASHION STORE — Admin Core Utilities

function toggleSidebarMode() {
  const sb = document.getElementById('v22Sidebar');
  if (!sb) return;
  sb.classList.toggle('pill-mode');
  const isPill = sb.classList.contains('pill-mode');
  localStorage.setItem('v22_sidebar_mode', isPill ? 'pill' : 'expanded');
}

function toggleDarkLight() {
  const html = document.documentElement;
  const current = html.getAttribute('data-theme') || 'v22-mint';
  const next = current === 'v22-dark' ? 'v22-mint' : 'v22-dark';
  html.setAttribute('data-theme', next);
  localStorage.setItem('v22_theme', next);

  const icon = document.getElementById('themeIcon');
  if (icon) icon.textContent = next === 'v22-dark' ? '☀️' : '🌙';
}

let v22ToastTimer;
function showToast(msg) {
  let toast = document.getElementById('v22Toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'v22Toast';
    toast.innerHTML = '<span class="toast-icon">✓</span> <span id="v22ToastMsg"></span>';
    document.body.appendChild(toast);
  }
  const txt = document.getElementById('v22ToastMsg');
  if (txt) txt.textContent = msg;
  toast.classList.add('show');
  clearTimeout(v22ToastTimer);
  v22ToastTimer = setTimeout(() => {
    toast.classList.remove('show');
  }, 2800);
}

function toggleKeyVisibility(inputId) {
  const el = document.getElementById(inputId);
  if (el) {
    el.type = el.type === 'password' ? 'text' : 'password';
  }
}

function copyKeyField(inputId, label) {
  const el = document.getElementById(inputId);
  if (!el) return;
  const prev = el.type;
  el.type = 'text';
  el.select();
  try {
    navigator.clipboard.writeText(el.value).then(() => {
      showToast(`Đã sao chép ${label || 'khóa'} vào Clipboard!`);
    }).catch(() => {
      document.execCommand('copy');
      showToast(`Đã sao chép ${label || 'khóa'} vào Clipboard!`);
    });
  } catch (e) {
    document.execCommand('copy');
    showToast(`Đã sao chép ${label || 'khóa'} vào Clipboard!`);
  }
  el.type = prev;
}

function animateCountUp() {
  document.querySelectorAll('.count-up').forEach(el => {
    const rawVal = el.getAttribute('data-val');
    if (!rawVal) return;
    const target = parseInt(rawVal.replace(/[^\d]/g, ''), 10) || 0;
    let cur = 0;
    const inc = Math.max(1, Math.ceil(target / 24));
    const timer = setInterval(() => {
      cur += inc;
      if (cur >= target) {
        cur = target;
        clearInterval(timer);
      }
      el.textContent = cur.toLocaleString('vi-VN');
    }, 18);
  });
}

function setProductDisplayMode(mode) {
  const bento = document.getElementById('productsBentoContainer');
  const tbl = document.getElementById('productsTableContainer');
  const btnBento = document.getElementById('prodModeBento');
  const btnTbl = document.getElementById('prodModeTable');

  if (bento && tbl) {
    bento.style.display = mode === 'bento' ? '' : 'none';
    tbl.style.display = mode === 'table' ? 'block' : 'none';
  }

  if (btnBento && btnTbl) {
    if (mode === 'bento') {
      btnBento.classList.add('active');
      btnTbl.classList.remove('active');
    } else {
      btnTbl.classList.add('active');
      btnBento.classList.remove('active');
    }
  }

  localStorage.setItem('v22_product_display_mode', mode);
}

function toggleOrderViewMode(mode) {
  const tbl = document.getElementById('ordersTableView');
  const kanban = document.getElementById('ordersKanbanView');
  const btnTbl = document.getElementById('ordViewTableBtn');
  const btnKanban = document.getElementById('ordViewKanbanBtn');

  if (tbl && kanban) {
    tbl.style.display = mode === 'table' ? 'block' : 'none';
    kanban.style.display = mode === 'kanban' ? '' : 'none';
  }

  if (btnTbl && btnKanban) {
    if (mode === 'table') {
      btnTbl.classList.add('active');
      btnKanban.classList.remove('active');
    } else {
      btnKanban.classList.add('active');
      btnTbl.classList.remove('active');
    }
  }

  localStorage.setItem('v22_order_view_mode', mode);
}

document.addEventListener('DOMContentLoaded', function () {
  // Restore theme
  const savedTheme = localStorage.getItem('v22_theme') || 'v22-mint';
  document.documentElement.setAttribute('data-theme', savedTheme);
  const icon = document.getElementById('themeIcon');
  if (icon) icon.textContent = savedTheme === 'v22-dark' ? '☀️' : '🌙';

  // Restore sidebar mode
  const savedSb = localStorage.getItem('v22_sidebar_mode');
  if (savedSb === 'pill') {
    const sb = document.getElementById('v22Sidebar');
    if (sb) sb.classList.add('pill-mode');
  }

  // Restore product display mode if on products page
  const savedProdMode = localStorage.getItem('v22_product_display_mode');
  if (savedProdMode) {
    setProductDisplayMode(savedProdMode);
  }

  // Restore order view mode if on orders page
  const savedOrdMode = localStorage.getItem('v22_order_view_mode');
  if (savedOrdMode) {
    toggleOrderViewMode(savedOrdMode);
  }

  // Animate counters
  animateCountUp();
});
