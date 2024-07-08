$(document).ready(function () {
    $('.delete-btn').on('click', function (e) {
        e.preventDefault(); 

        var url = $(this).attr('href'); 
        var id = $(this).data('id'); 

        Swal.fire({
            title: 'Tem certeza?',
            text: 'Esta ação não pode ser desfeita!',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Sim, excluir!',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: url,
                    type: 'POST', 
                    data: { id: id }, 
                    success: function (response) {
                        if (response.success) {
                            Swal.fire({
                                position: "top-end",
                                icon: "success",
                                title: "Registro excluído com sucesso",
                                showConfirmButton: false,
                                timer: 1500
                            });
                            location.reload();                            
                        } else {
                            console.log('Erro: ' + response.errorMessage);
                            Swal.fire({
                                icon: 'error',
                                title: 'Erro!',
                                text: 'Este produto está vinculado a um pedido. Não pode ser excluído.',
                                confirmButtonText: 'OK'
                            });
                        }
                    },
                    error: function (xhr, status, error) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Erro!',
                            text: error,
                            confirmButtonText: 'OK'
                        }); 
                    }
                });
            }
        });
    });
});
