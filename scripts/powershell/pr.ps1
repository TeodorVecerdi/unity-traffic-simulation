function pr {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Title
    )

    gh pr create -b "" -a "@me" -t "$Title"
}