export function DialogModule(modal) {
    const listener = (e) => {
        if (!modal.open) return;
        e.preventDefault();
        history.go(1);
        modal.close();
        modal.invokeMethodAsync("CancelFromJs");
    }

    return {
        dispose: () => window.removeEventListener("popstate", listener),
        listen: () => window.addEventListener("popstate", listener),
        open: () => modal.showModal(),
        close: () => modal.close()
    }
}