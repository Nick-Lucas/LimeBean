param ([string]$build_number, [string]$tag)

$meta_version_numeric = "0.0"

if ($tag -match '^([.\d]+)[\w-]*')  {
    $meta_version_full = $matches[0]
    $meta_version_numeric = $matches[1]
} elseif ($build_number) {
    $meta_version_full = "$meta_version_numeric-ci-build$build_number"
} else {
    $meta_version_full = $meta_version_numeric
}

$meta_description = "RedBeanPHP-inspired data access layer"
$meta_author = "Aleksey Martynov"
$meta_copyright = "Copyright (c) 2014-$(get-date -format yyyy) $meta_author"
$meta_project_url = "http://www.limebean.net/"
$meta_license_url = "https://raw.githubusercontent.com/AlekseyMartynov/LimeBean/master/LICENSE.txt"

("LimeBean\AssemblyInfo.cs", "LimeBean.Dnx\LimeBean\project.json") | %{
    $path = "$PSScriptRoot\$_"

    (Get-Content $path) | %{
        $_  -replace '(AssemblyVersion.+?")[^"]+', "`${1}$meta_version_numeric" `
            -replace '("version":.+?")[^"]+', "`${1}$meta_version_full" `
            -replace '%lime_description%', $meta_description `
            -replace '%lime_author%', $meta_author `
            -replace '%lime_copyright%', $meta_copyright `
            -replace '%lime_project_url%', $meta_project_url `
            -replace '%lime_license_url%', $meta_license_url
    } | Set-Content $path
}
