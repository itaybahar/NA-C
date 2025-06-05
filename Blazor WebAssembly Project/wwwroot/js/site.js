window.fadeOutUserInfo = function () {
    return new Promise((resolve) => {
        const userInfo = document.querySelector('.user-info');
        const iconWrapper = document.querySelector('.icon-wrapper.success');
        
        if (userInfo) {
            userInfo.classList.remove('show');
            userInfo.addEventListener('transitionend', function handler() {
                userInfo.style.display = 'none';
                userInfo.removeEventListener('transitionend', handler);
            });
        }
        
        if (iconWrapper) {
            iconWrapper.style.opacity = '0';
            iconWrapper.addEventListener('transitionend', function handler() {
                iconWrapper.style.display = 'none';
                iconWrapper.removeEventListener('transitionend', handler);
                resolve();
            });
        } else {
            resolve();
        }
        
        // Fallback in case transition doesn't fire
        setTimeout(resolve, 600);
    });
} 