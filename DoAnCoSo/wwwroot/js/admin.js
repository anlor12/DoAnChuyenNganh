document.addEventListener('DOMContentLoaded', function () {
    // 1. Live Search Logic for all Admin Index pages
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keyup', function () {
            const filter = searchInput.value.toLowerCase();
            const table = document.querySelector('table');
            
            if (table) {
                const tr = table.getElementsByTagName('tr');
                for (let i = 1; i < tr.length; i++) {
                    const th = tr[i].getElementsByTagName("th");
                    if (th.length > 0) continue; 

                    const rowText = tr[i].textContent || tr[i].innerText;
                    if (rowText.toLowerCase().indexOf(filter) > -1) {
                        tr[i].style.display = "";
                    } else {
                        tr[i].style.display = "none";
                    }
                }
            }
        });
    }

    // 2. Toggle Sidebar (Mobile / Responsive)
    const sidebarToggle = document.getElementById('sidebarToggle');
    const adminSidebar = document.getElementById('adminSidebar');
    if (sidebarToggle && adminSidebar) {
        sidebarToggle.addEventListener('click', function () {
            if (adminSidebar.style.width === '0px' || adminSidebar.style.display === 'none') {
                adminSidebar.style.width = '250px';
                adminSidebar.style.display = 'flex';
            } else {
                adminSidebar.style.width = '0px';
                adminSidebar.style.display = 'none';
            }
        });
    }

    // 3. Toggle Multilevel Submenus
    document.querySelectorAll('.menu-toggle-btn').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('data-target');
            const targetMenu = document.getElementById(targetId);
            const chevron = this.querySelector('.chevron-icon');
            if (targetMenu) {
                const isOpen = targetMenu.style.display === 'flex' || window.getComputedStyle(targetMenu).display === 'flex';
                if (isOpen) {
                    targetMenu.style.display = 'none';
                    if (chevron) chevron.style.transform = 'rotate(0deg)';
                } else {
                    targetMenu.style.display = 'flex';
                    if (chevron) chevron.style.transform = 'rotate(180deg)';
                }
            }
        });
    });
});
