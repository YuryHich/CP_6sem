function checkAuth() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const username = localStorage.getItem('username');

    if (token) {
        document.getElementById('loginNav').style.display = 'none';
        document.getElementById('registerNav').style.display = 'none';
        document.getElementById('logoutNav').style.display = 'block';
        document.getElementById('deleteAccountNav').style.display = 'block';
        document.getElementById('loansNav').style.display = 'block';
        
        const statisticsNav = document.getElementById('statisticsNav');
        if (statisticsNav) {
            statisticsNav.style.display = 'block';
        }
        
        if (role === 'admin') {
            document.getElementById('adminNav').style.display = 'block';
            const lookupsNav = document.getElementById('lookupsNav');
            if (lookupsNav) {
                lookupsNav.style.display = 'block';
            }
            const historyNav = document.getElementById('historyNav');
            if (historyNav) {
                historyNav.style.display = 'block';
            }
            const exportNav = document.getElementById('exportNav');
            if (exportNav) {
                exportNav.style.display = 'block';
            }
            const usersNav = document.getElementById('usersNav');
            if (usersNav) {
                usersNav.style.display = 'block';
            }
        }
    } else {
        document.getElementById('loginNav').style.display = 'block';
        document.getElementById('registerNav').style.display = 'block';
        document.getElementById('logoutNav').style.display = 'none';
        document.getElementById('deleteAccountNav').style.display = 'none';
        document.getElementById('loansNav').style.display = 'none';
        document.getElementById('adminNav').style.display = 'none';
        
        const statisticsNav = document.getElementById('statisticsNav');
        if (statisticsNav) {
            statisticsNav.style.display = 'none';
        }
        
        const reportsNav = document.getElementById('reportsNav');
        if (reportsNav) {
            reportsNav.style.display = 'none';
        }
        const lookupsNav = document.getElementById('lookupsNav');
        if (lookupsNav) {
            lookupsNav.style.display = 'none';
        }
        const exportNav = document.getElementById('exportNav');
        if (exportNav) {
            exportNav.style.display = 'none';
        }
        const historyNav = document.getElementById('historyNav');
        if (historyNav) {
            historyNav.style.display = 'none';
        }
        const usersNav = document.getElementById('usersNav');
        if (usersNav) {
            usersNav.style.display = 'none';
        }
    }
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('username');
    localStorage.removeItem('role');
    window.location.href = '/';
}

// Проверяем авторизацию при загрузке страницы
document.addEventListener('DOMContentLoaded', checkAuth);
