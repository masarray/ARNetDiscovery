
const revealItems = document.querySelectorAll('.reveal');
const io = new IntersectionObserver((entries)=>{
  for (const entry of entries) {
    if (entry.isIntersecting) entry.target.classList.add('visible');
  }
},{threshold:.12});
revealItems.forEach(el=>io.observe(el));

const modal = document.querySelector('[data-lightbox]');
const modalImage = modal?.querySelector('img');
const closeBtn = modal?.querySelector('[data-close]');
function openLightbox(src, alt){
  if(!modal || !modalImage) return;
  modalImage.src = src;
  modalImage.alt = alt || 'ARNet Discovery screenshot';
  modal.classList.add('open');
  document.body.style.overflow='hidden';
}
function closeLightbox(){
  if(!modal) return;
  modal.classList.remove('open');
  document.body.style.overflow='';
}
document.querySelectorAll('[data-zoom]').forEach(img=>{
  img.addEventListener('click',()=>openLightbox(img.currentSrc || img.src, img.alt));
});
closeBtn?.addEventListener('click', closeLightbox);
modal?.addEventListener('click', (event)=>{ if(event.target === modal) closeLightbox(); });
document.addEventListener('keydown', (event)=>{ if(event.key === 'Escape') closeLightbox(); });

const navLinks = Array.from(document.querySelectorAll('.navlinks a[href^="#"]'));
const sections = navLinks.map(a=>document.querySelector(a.getAttribute('href'))).filter(Boolean);
const navObserver = new IntersectionObserver((entries)=>{
  const visible = entries.filter(e=>e.isIntersecting).sort((a,b)=>b.intersectionRatio-a.intersectionRatio)[0];
  if(!visible) return;
  navLinks.forEach(a=>a.classList.toggle('active', a.getAttribute('href') === `#${visible.target.id}`));
},{threshold:.4});
sections.forEach(s=>navObserver.observe(s));
