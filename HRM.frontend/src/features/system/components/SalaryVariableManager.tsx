import React, { useState } from "react";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type {
  CreateSourceCatalogPayload,
  SalaryVariable,
} from "../types/salaryVariable";

const emptyVariable: SalaryVariable = {
  code: "",
  source: "",
  description: "",
};

const emptyCatalog: CreateSourceCatalogPayload = {
  displayName: "",
  sourcePath: "",
  module: "Payroll",
  dataType: "Decimal",
  aggregationType: "Sum",
  isPeriodBased: true,
  isActive: true,
};

export const SalaryVariableManager: React.FC = () => {
  const {
    variables,
    catalogs,
    loading,
    defineVariable,
    createCatalog,
  } = useSalaryVariable();

  const [variableForm, setVariableForm] =
    useState<SalaryVariable>(emptyVariable);
  const [catalogForm, setCatalogForm] =
    useState<CreateSourceCatalogPayload>(emptyCatalog);
  const [message, setMessage] = useState<string>("");

  const handleVariableChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = e.target;
    setVariableForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCatalogChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = e.target;
    const checked =
      type === "checkbox" ? (e.target as HTMLInputElement).checked : undefined;

    setCatalogForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleVariableSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await defineVariable(variableForm);
      setMessage(res.message || "Da luu bien luong.");
      setVariableForm(emptyVariable);
    } catch (error: unknown) {
      setMessage(`Loi: ${String(error)}`);
    }
  };

  const handleCatalogSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await createCatalog(catalogForm);
      setMessage(res.message || "Da them nguon du lieu luong.");
      setCatalogForm(emptyCatalog);
    } catch (error: unknown) {
      setMessage(`Loi: ${String(error)}`);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h2 className="mb-4 text-xl font-bold">
          Quan ly bien luong he thong (F0.1)
        </h2>

        <form
          onSubmit={handleVariableSubmit}
          className="mb-6 grid grid-cols-1 items-end gap-4 md:grid-cols-4"
        >
          <div>
            <label className="mb-1 block text-sm font-medium">
              Ma bien (Code) *
            </label>
            <input
              required
              name="code"
              value={variableForm.code}
              onChange={handleVariableChange}
              pattern="^[a-zA-Z0-9_]+$"
              title="Chi dung chu, so va dau gach duoi"
              className="w-full rounded border p-2"
              placeholder="vd: ot_hours"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">
              Nguon du lieu (Source) *
            </label>
            <select
              required
              name="source"
              value={variableForm.source}
              onChange={handleVariableChange}
              className="w-full rounded border bg-white p-2"
            >
              <option value="" disabled>
                -- Chon source catalog --
              </option>
              {catalogs
                .filter((item) => item.isActive)
                .map((item) => (
                  <option key={item.id} value={item.sourcePath}>
                    [{item.module}] {item.displayName} - {item.dataType}/
                    {item.aggregationType}
                  </option>
                ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Mo ta</label>
            <input
              name="description"
              value={variableForm.description}
              onChange={handleVariableChange}
              className="w-full rounded border p-2"
              placeholder="vd: Gio tang ca hop le"
            />
          </div>

          <button
            type="submit"
            className="rounded bg-blue-600 p-2 text-white hover:bg-blue-700"
          >
            Luu bien luong
          </button>
        </form>

        {message && (
          <p className="mb-4 text-sm font-medium text-blue-600">{message}</p>
        )}

        {loading ? (
          <p className="py-4 text-center">Dang tai du lieu...</p>
        ) : (
          <table className="w-full border-collapse border text-left">
            <thead className="bg-gray-100">
              <tr>
                <th className="border p-2">Code</th>
                <th className="border p-2">Source</th>
                <th className="border p-2">Mo ta</th>
              </tr>
            </thead>
            <tbody>
              {variables.length > 0 ? (
                variables.map((item, index) => (
                  <tr key={`${item.code}-${index}`} className="hover:bg-gray-50">
                    <td className="border p-2 font-mono text-blue-600">
                      {item.code}
                    </td>
                    <td className="border p-2 font-mono">{item.source}</td>
                    <td className="border p-2">{item.description}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={3} className="p-4 text-center text-gray-500">
                    Chua co bien luong nao duoc dinh nghia.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h3 className="mb-4 text-lg font-semibold">
          Mo rong nguon du lieu luong
        </h3>

        <form
          onSubmit={handleCatalogSubmit}
          className="grid grid-cols-1 gap-4 md:grid-cols-3"
        >
          <div>
            <label className="mb-1 block text-sm font-medium">
              Ten hien thi *
            </label>
            <input
              required
              name="displayName"
              value={catalogForm.displayName}
              onChange={handleCatalogChange}
              className="w-full rounded border p-2"
              placeholder="vd: So phut OT hop le"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">
              Source path *
            </label>
            <input
              required
              name="sourcePath"
              value={catalogForm.sourcePath}
              onChange={handleCatalogChange}
              className="w-full rounded border p-2"
              placeholder="vd: Overtime.ActualOtMinutes"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Module</label>
            <input
              name="module"
              value={catalogForm.module}
              onChange={handleCatalogChange}
              className="w-full rounded border p-2"
              placeholder="Payroll"
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Data type</label>
            <select
              name="dataType"
              value={catalogForm.dataType}
              onChange={handleCatalogChange}
              className="w-full rounded border bg-white p-2"
            >
              <option value="Decimal">Decimal</option>
              <option value="Integer">Integer</option>
              <option value="Boolean">Boolean</option>
              <option value="Date">Date</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">
              Aggregation
            </label>
            <select
              name="aggregationType"
              value={catalogForm.aggregationType}
              onChange={handleCatalogChange}
              className="w-full rounded border bg-white p-2"
            >
              <option value="Sum">Sum</option>
              <option value="Average">Average</option>
              <option value="Latest">Latest</option>
              <option value="Count">Count</option>
              <option value="None">None</option>
            </select>
          </div>

          <div className="flex items-center gap-6 pt-7">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                name="isPeriodBased"
                checked={catalogForm.isPeriodBased}
                onChange={handleCatalogChange}
              />
              Theo ky luong
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                name="isActive"
                checked={catalogForm.isActive}
                onChange={handleCatalogChange}
              />
              Dang dung
            </label>
          </div>

          <div className="md:col-span-3">
            <button
              type="submit"
              className="rounded bg-gray-900 px-4 py-2 text-white hover:bg-gray-800"
            >
              Them source catalog
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
