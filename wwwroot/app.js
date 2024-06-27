document.addEventListener('DOMContentLoaded', () => {
    const asideToggler = () => {
        const aside = document.querySelector('.aside')
        const headerBtn = document.querySelector('.aside-btn')

        if (!aside || !headerBtn) {
            return
        }

        headerBtn.addEventListener('click', () => {
            aside.classList.toggle('active')
            headerBtn.classList.toggle('active')
        })
    }
    asideToggler()
})