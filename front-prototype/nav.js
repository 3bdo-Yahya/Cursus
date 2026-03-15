/**
 * Cursus Prototype Navigation Injector
 * Automatically replaces placeholder nav links (href="#") with real page links.
 * Include this script at the bottom of each page's code.html via <script src="../nav.js"></script>
 */

(function () {
    // Define navigation routes by role
    const studentNav = {
        'Dashboard': '../student_dashboard_concept_1/code.html',
        'Course Map': '../course_map_knowledge_graph_variant/code.html',
        'Impact Analyzer': '../impact_analyzer_split_panel_variant/code.html',
        'Progress & Planning': '../progress_planning_tracker/code.html',
        'Progress Tracker': '../progress_planning_tracker/code.html',
        'GPA Simulator': '../gpa_simulator_split_pane_dashboard_variant/code.html',
        'AI Advisor': '../ai_advisor_empty_state/code.html',
    };

    const adminNav = {
        'Dashboard': '../admin_dashboard_standard_layout/code.html',
        'Courses': '../course_list_standard/code.html',
        'Students': '../student_management_list_view/code.html',
        'Settings': '#',
    };

    const sharedLinks = {
        'Profile': '../profile_settings_standard_layout/code.html',
        'Login': '../updated_login_page_split_screen/code.html',
        'Sign In': '../updated_login_page_split_screen/code.html',
    };

    // Admin sub-page links
    const adminSubLinks = {
        'Manage Courses': '../course_list_standard/code.html',
        'Manage Students': '../student_management_list_view/code.html',
        'Add Course': '../course_form_standard/code.html',
        'View Full Report': '../course_list_standard/code.html',
        'Notify Dept': '#',
    };

    // Detect which role context we're in based on page title or nav content
    const pageTitle = document.title.toLowerCase();
    const bodyHTML = document.body.innerHTML.toLowerCase();
    const isAdmin = pageTitle.includes('admin') ||
        bodyHTML.includes('manage courses') ||
        bodyHTML.includes('admin dashboard') ||
        (bodyHTML.includes('>courses<') && bodyHTML.includes('>students<') && !bodyHTML.includes('course map'));
    const isLogin = pageTitle.includes('sign in') || pageTitle.includes('login');

    // Build the link map for this page
    const linkMap = { ...sharedLinks };
    if (isAdmin) {
        Object.assign(linkMap, adminNav, adminSubLinks);
    } else if (!isLogin) {
        Object.assign(linkMap, studentNav);
    }

    // Find all anchor tags and update hrefs
    const allLinks = document.querySelectorAll('a[href="#"], a[href=""], a:not([href])');
    allLinks.forEach(function (link) {
        const text = link.textContent.trim();
        // Try exact match first
        if (linkMap[text]) {
            link.href = linkMap[text];
            return;
        }
        // Try partial match
        for (const [label, url] of Object.entries(linkMap)) {
            if (text.includes(label) || label.includes(text)) {
                link.href = url;
                return;
            }
        }
    });

    // Update clickable cards/divs that look like navigation (admin quick actions)
    const clickableCards = document.querySelectorAll('[class*="cursor-pointer"]');
    clickableCards.forEach(function (card) {
        const text = card.textContent.trim();
        for (const [label, url] of Object.entries(linkMap)) {
            if (text.includes(label)) {
                card.style.cursor = 'pointer';
                card.addEventListener('click', function () {
                    window.location.href = url;
                });
                break;
            }
        }
    });

    // Make avatar/profile icon clickable
    const avatarElements = document.querySelectorAll('[class*="rounded-full"][class*="overflow-hidden"]');
    avatarElements.forEach(function (el) {
        if (el.querySelector('img') && el.closest('nav')) {
            el.style.cursor = 'pointer';
            el.title = 'Profile Settings';
            el.addEventListener('click', function () {
                window.location.href = linkMap['Profile'] || '#';
            });
        }
    });

    // Add a floating "Back to Prototype Hub" button
    const hubBtn = document.createElement('a');
    hubBtn.href = '../index.html';
    hubBtn.innerHTML = '<span class="material-symbols-outlined" style="font-size:18px;vertical-align:middle;margin-right:4px">grid_view</span>All Pages';
    hubBtn.style.cssText = 'position:fixed;bottom:20px;right:20px;z-index:9999;background:#4F46E5;color:white;padding:10px 18px;border-radius:12px;text-decoration:none;font-family:Public Sans,sans-serif;font-size:13px;font-weight:600;box-shadow:0 4px 12px rgba(79,70,229,0.4);display:flex;align-items:center;transition:all 0.2s;';
    hubBtn.addEventListener('mouseenter', function () { this.style.transform = 'scale(1.05)'; this.style.boxShadow = '0 6px 16px rgba(79,70,229,0.5)'; });
    hubBtn.addEventListener('mouseleave', function () { this.style.transform = 'scale(1)'; this.style.boxShadow = '0 4px 12px rgba(79,70,229,0.4)'; });
    document.body.appendChild(hubBtn);

    // Also make the Cursus logo link back to role-appropriate dashboard
    const logoLinks = document.querySelectorAll('h1, h2');
    logoLinks.forEach(function (el) {
        if (el.textContent.trim() === 'Cursus') {
            const parent = el.closest('a') || el.closest('div');
            if (parent && parent.closest('nav')) {
                parent.style.cursor = 'pointer';
                parent.addEventListener('click', function () {
                    window.location.href = isAdmin ? linkMap['Dashboard'] : (linkMap['Dashboard'] || '../index.html');
                });
            }
        }
    });

    // Make the login form redirect to student dashboard
    const loginForm = document.querySelector('form');
    if (isLogin && loginForm) {
        loginForm.addEventListener('submit', function (e) {
            e.preventDefault();
            window.location.href = '../student_dashboard_concept_1/code.html';
        });
    }

    console.log('[Cursus Nav] Navigation injected. Role: ' + (isLogin ? 'Login' : isAdmin ? 'Admin' : 'Student'));
})();
