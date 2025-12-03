function checkAuth() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const username = localStorage.getItem('username');

    if (token) {
        document.getElementById('loginNav').style.display = 'none';
        document.getElementById('registerNav').style.display = 'none';
        document.getElementById('logoutNav').style.display = 'block';
        document.getElementById('loansNav').style.display = 'block';
        
        if (role === 'admin') {
            document.getElementById('adminNav').style.display = 'block';
            document.getElementById('reportsNav').style.display = 'block';
        }
    } else {
        document.getElementById('loginNav').style.display = 'block';
        document.getElementById('registerNav').style.display = 'block';
        document.getElementById('logoutNav').style.display = 'none';
        document.getElementById('loansNav').style.display = 'none';
        document.getElementById('adminNav').style.display = 'none';
        document.getElementById('reportsNav').style.display = 'none';
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

