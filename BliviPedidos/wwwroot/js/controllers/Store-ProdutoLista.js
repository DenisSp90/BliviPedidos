$(document).ready(function () {
    $(document).on('click', '.delete-btn', function (e) {
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
                    data: {
                        id: id,
                        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').first().val()
                    }, 
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
                            Swal.fire({
                                icon: 'error',
                                title: 'Erro!',
                                text: response.errorMessage || 'Não foi possível excluir o produto.',
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
