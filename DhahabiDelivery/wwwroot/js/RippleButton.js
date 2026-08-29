export function createRipple(button, componentInstance, RippleColor) {
    button.onclick = async function (e) {
        //let x = e.clientX - e.target.offsetLeft
        //let y = e.clientY - e.target.offsetTop
        let rect = e.target.getBoundingClientRect();
        let top = e.clientY - rect.top;
        let left = e.clientX - rect.left;
        //console.log("x", x, "y", y, "clientX", e.clientX, "clientY", e.clientY, "offsetLeft", e.target.offsetLeft, "offsetTop", e.target.offsetTop)
        let ripples = document.createElement('span')
        ripples.style.left = `${left}px`
        ripples.classList.add('ripple-span')
        ripples.style.top = `${top}px`
        ripples.style.background = RippleColor
        this.appendChild(ripples)
        setTimeout(() => {
            ripples.remove()
        }, 600)
        await componentInstance.invokeMethodAsync("OnTouch");
    }
}