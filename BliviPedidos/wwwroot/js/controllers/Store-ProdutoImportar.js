$(document).ready(function () {
    $('#uploadButton').click(function () {
        var fileInput = $('#file')[0];
        var file = fileInput.files[0];

        if (file) {
            // Verifique a extensão do arquivo
            var fileName = file.name;
            var extension = fileName.substr((fileName.lastIndexOf('.') + 1)).toLowerCase();
            if (extension !== 'xlsx') {
                alert('Please upload a file with .xlsx extension.');
                return;
            }

            var formData = new FormData();
            formData.append('file', file);

            $.ajax({
                url: '/Store/ProdutoImportarUpload', // Atualize a URL conforme necessário
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                success: function (response) {
                    
                    var produtos = JSON.parse(response);
                    var tbody = $('table tbody');

                    tbody.empty(); // Limpar quaisquer linhas existentes

                    produtos.forEach(function (produto, index) {
                        var lucro = (produto.PrecoVenda - produto.PrecoPago) * produto.Quantidade;
                        var row = `
                    <tr class="table-success">
                        <th scope="row">${index + 1}</th>
                        <td>${produto.Nome}</td>
                        <td>${produto.PrecoPago}</td>
                        <td>${produto.PrecoVenda}</td>
                        <td>${produto.Quantidade}</td>
                        <td>${lucro.toFixed(2)}</td>
                    </tr>
                `;
                        tbody.append(row);
                    });

                    Swal.fire({
                        icon: 'success',
                        title: 'Sucesso',
                        text: 'Os dados foram carregados. Deseja recarregar a página para obter o valor total dos lucros dos pedidos?',
                        showCancelButton: true,
                        confirmButtonText: 'Sim, recarregar',
                        cancelButtonText: 'Não, continuar'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            location.reload();
                        }
                    });
                },
                error: function (jqXHR, textStatus, errorThrown) {

                    console.log(textStatus, errorThrown);

                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: 'Arquivo não está válido. Por favor, verifique e tente novamente.',
                    });

                }
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Erro',
                text: 'Arquivo não está selecionado. Por favor, verifique e tente novamente.',
            });
        }
    });

    $('#downloadButton').click(function () {
        $.ajax({
            url: '/Store/ProdutoDownloadListaImportacao',
            type: 'GET',
            xhrFields: {
                responseType: 'blob'  // Important
            },
            success: function (data, status, xhr) {
                var blob = new Blob([data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(blob);
                link.download = 'ProdutoListaImportacao.xlsx';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            },
            error: function (xhr, status, error) {
                console.error('Failed to download file:', error);
            }
        });
    });
});