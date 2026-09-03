$(document).ready(function () {
    $(document).on('click', '.pedido-delete-btn', function () {
        const id = $(this).data('id');
        const status = String($(this).data('status'));
        const podeDevolver = status !== 'Cancelado' && status !== 'Carrinho';
        let devolverEstoque = podeDevolver;

        Swal.fire({
            title: 'Excluir o pedido definitivamente?',
            html: podeDevolver
                ? '<p>O pedido e seus itens serão removidos.</p><label class="d-flex gap-2 justify-content-center align-items-center"><input id="devolverEstoque" type="checkbox" checked> Devolver os itens ao estoque</label>'
                : '<p>O pedido e seus itens serão removidos. O estoque não será alterado porque este pedido já foi cancelado ou ainda era um carrinho.</p>',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Excluir definitivamente',
            cancelButtonText: 'Cancelar',
            didOpen: function () {
                const checkbox = document.getElementById('devolverEstoque');
                if (checkbox) {
                    devolverEstoque = checkbox.checked;
                    checkbox.addEventListener('change', function () {
                        devolverEstoque = checkbox.checked;
                    });
                }
            }
        }).then(function (resultado) {
            if (!resultado.isConfirmed) return;

            $.ajax({
                url: '/Store/PedidoDelete',
                type: 'POST',
                data: {
                    id: id,
                    devolverEstoque: devolverEstoque,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').first().val()
                },
                success: function (response) {
                    if (!response.success) {
                        Swal.fire('Erro!', response.errorMessage || 'Não foi possível excluir o pedido.', 'error');
                        return;
                    }

                    Swal.fire({
                        icon: 'success',
                        title: 'Pedido excluído',
                        text: devolverEstoque ? 'Os itens foram devolvidos ao estoque.' : 'O estoque não foi alterado.',
                        showConfirmButton: false,
                        timer: 1800
                    }).then(function () { location.reload(); });
                },
                error: function () {
                    Swal.fire('Erro!', 'Não foi possível excluir o pedido.', 'error');
                }
            });
        });
    });
});
