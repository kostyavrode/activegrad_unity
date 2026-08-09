(function () {
  'use strict';

  const header = document.querySelector('.header');
  const navToggle = document.querySelector('.nav__toggle');
  const navMenu = document.querySelector('.nav__menu');
  const form = document.getElementById('partnership-form');
  const formStatus = document.getElementById('form-status');
  const submitBtn = document.getElementById('submit-btn');

  /* Sticky header with blur */
  function onScroll() {
    header.classList.toggle('is-scrolled', window.scrollY > 16);
  }
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  /* Mobile nav */
  if (navToggle && navMenu) {
    navToggle.addEventListener('click', () => {
      const open = navToggle.getAttribute('aria-expanded') === 'true';
      navToggle.setAttribute('aria-expanded', String(!open));
      navMenu.classList.toggle('is-open', !open);
    });

    navMenu.querySelectorAll('a').forEach((link) => {
      link.addEventListener('click', () => {
        navToggle.setAttribute('aria-expanded', 'false');
        navMenu.classList.remove('is-open');
      });
    });
  }

  /* Scroll reveal */
  const revealEls = document.querySelectorAll('.reveal');
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
    );
    revealEls.forEach((el) => observer.observe(el));
  } else {
    revealEls.forEach((el) => el.classList.add('is-visible'));
  }

  /* Form validation messages */
  const messages = {
    required: 'Обязательное поле',
    email: 'Введите корректный email',
    consent: 'Необходимо согласие на обработку данных',
  };

  function validateField(field) {
    const wrapper = field.closest('.form-field');
    const errorEl = wrapper?.querySelector('.form-error');
    if (!errorEl) return true;

    let valid = true;
    let message = '';

    if (field.type === 'checkbox') {
      valid = field.checked;
      message = valid ? '' : messages.consent;
    } else if (field.required && !field.value.trim()) {
      valid = false;
      message = messages.required;
    } else if (field.type === 'email' && field.value.trim()) {
      valid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(field.value.trim());
      message = valid ? '' : messages.email;
    }

    field.classList.toggle('is-invalid', !valid);
    errorEl.textContent = message;
    return valid;
  }

  form?.querySelectorAll('input, select, textarea').forEach((field) => {
    field.addEventListener('blur', () => validateField(field));
    field.addEventListener('input', () => {
      if (field.classList.contains('is-invalid')) validateField(field);
    });
  });

  /* Form submit */
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();

    const fields = [...form.querySelectorAll('input, select, textarea')];
    const allValid = fields.every((f) => validateField(f));
    if (!allValid) {
      setStatus('Проверьте поля формы', 'error');
      const firstInvalid = form.querySelector('.is-invalid');
      firstInvalid?.focus();
      return;
    }

    const endpoint = form.dataset.endpoint;
    if (!endpoint || endpoint.includes('YOUR_FORM_ID')) {
      setStatus(
        'Форма ещё не настроена. Укажите endpoint в data-endpoint (Formspree / Web3Forms / ваш API).',
        'error'
      );
      return;
    }

    submitBtn.classList.add('is-loading');
    submitBtn.disabled = true;
    setStatus('Отправляем…', '');

    const payload = Object.fromEntries(new FormData(form).entries());
    payload._subject = `ActiveGrad — заявка от ${payload.company || payload.name}`;

    try {
      const res = await fetch(endpoint, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (!res.ok) throw new Error('Request failed');

      form.reset();
      fields.forEach((f) => f.classList.remove('is-invalid'));
      setStatus('Заявка отправлена! Мы свяжемся с вами в ближайшее время.', 'success');
    } catch {
      setStatus('Не удалось отправить. Попробуйте позже или напишите на email.', 'error');
    } finally {
      submitBtn.classList.remove('is-loading');
      submitBtn.disabled = false;
    }
  });

  function setStatus(text, type) {
    if (!formStatus) return;
    formStatus.textContent = text;
    formStatus.className = 'form-status';
    if (type) formStatus.classList.add(`is-${type}`);
  }
})();
