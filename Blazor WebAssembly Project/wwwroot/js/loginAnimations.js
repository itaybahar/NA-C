window.loginAnimations = {
    initializeParticles: function() {
        const overlay = document.querySelector('.success-overlay');
        if (!overlay) return;

        // Create and append canvas for particles
        const canvas = document.createElement('canvas');
        canvas.style.position = 'absolute';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.pointerEvents = 'none';
        overlay.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        let particles = [];

        function resizeCanvas() {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
        }

        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);

        class Particle {
            constructor() {
                this.reset();
            }

            reset() {
                this.x = Math.random() * canvas.width;
                this.y = Math.random() * canvas.height;
                this.size = Math.random() * 3 + 1; // Smaller particles
                this.speedX = Math.random() * 2 - 1; // Slower movement
                this.speedY = Math.random() * 2 - 1;
                this.opacity = Math.random() * 0.3 + 0.1; // More subtle opacity
                this.growthRate = Math.random() * 0.02 + 0.01;
                this.maxSize = Math.random() * 4 + 2;
            }

            update() {
                this.x += this.speedX;
                this.y += this.speedY;
                
                // Gentle size pulsing
                this.size += this.growthRate;
                if (this.size > this.maxSize) {
                    this.growthRate = -Math.abs(this.growthRate);
                } else if (this.size < 1) {
                    this.growthRate = Math.abs(this.growthRate);
                }

                if (this.x < 0 || this.x > canvas.width || 
                    this.y < 0 || this.y > canvas.height) {
                    this.reset();
                }
            }

            draw() {
                ctx.beginPath();
                ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
                ctx.fillStyle = `rgba(255, 255, 255, ${this.opacity})`;
                ctx.fill();
            }
        }

        // Create fewer particles for better performance
        for (let i = 0; i < 30; i++) {
            particles.push(new Particle());
        }

        let animationFrame;
        function animate() {
            if (!overlay.classList.contains('show')) {
                cancelAnimationFrame(animationFrame);
                return;
            }
            
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            particles.forEach(particle => {
                particle.update();
                particle.draw();
            });

            animationFrame = requestAnimationFrame(animate);
        }

        // Start animation when overlay becomes visible
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.target.classList.contains('show')) {
                    animate();
                }
            });
        });

        observer.observe(overlay, {
            attributes: true,
            attributeFilter: ['class']
        });

        // Clean up on page unload
        window.addEventListener('unload', () => {
            observer.disconnect();
            if (animationFrame) {
                cancelAnimationFrame(animationFrame);
            }
        });
    }
}; 