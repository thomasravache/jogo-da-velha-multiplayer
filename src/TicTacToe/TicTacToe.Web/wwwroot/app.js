window.triggerConfetti = function () {
    const colors = ['#ff4757', '#00d2d3', '#f39c12', '#10b981'];
    const container = document.createElement('div');
    container.style.position = 'fixed';
    container.style.top = '0';
    container.style.left = '0';
    container.style.width = '100vw';
    container.style.height = '100vh';
    container.style.pointerEvents = 'none';
    container.style.zIndex = '9999';
    document.body.appendChild(container);

    for (let i = 0; i < 60; i++) {
        const confetto = document.createElement('div');
        confetto.style.position = 'absolute';
        confetto.style.width = (Math.random() * 8 + 6) + 'px';
        confetto.style.height = (Math.random() * 8 + 6) + 'px';
        confetto.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
        confetto.style.left = (Math.random() * 100) + 'vw';
        confetto.style.top = '-20px';
        confetto.style.opacity = '1';
        confetto.style.borderRadius = Math.random() > 0.5 ? '50%' : '2px';
        confetto.style.transition = 'transform 2.5s cubic-bezier(0.25, 0.46, 0.45, 0.94), opacity 2.5s ease-out';
        container.appendChild(confetto);

        setTimeout(() => {
            const destX = (Math.random() - 0.5) * 200;
            const destY = window.innerHeight + 50;
            const rot = Math.random() * 720 - 360;
            confetto.style.transform = `translate(${destX}px, ${destY}px) rotate(${rot}deg)`;
            confetto.style.opacity = '0';
        }, 20);
    }

    setTimeout(() => {
        container.remove();
    }, 2800);
};
