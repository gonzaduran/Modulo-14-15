from odoo import models, fields, api
from odoo.exceptions import ValidationError


class FichajeAsistencia(models.Model):
    _name = 'fichaje.asistencia'
    _description = 'Registro de Fichajes de DAM'

    name = fields.Char(
        string='Referencia',
        compute='_compute_name',
        store=True,
    )
    employee_id = fields.Many2one(
        'res.users',
        string='Empleado',
        default=lambda self: self.env.user,
        required=True,
    )
    fecha_fichaje = fields.Datetime(
        string='Fecha y Hora',
        default=fields.Datetime.now,
        required=True,
    )
    tipo_accion = fields.Selection(
        [
            ('entrada', 'Entrada'),
            ('salida', 'Salida'),
            ('descanso', 'Descanso'),
        ],
        string='Tipo de acción',
        required=True,
    )

    @api.depends('employee_id', 'fecha_fichaje')
    def _compute_name(self):
        for record in self:
            employee_name = record.employee_id.name or ''
            record.name = f"{employee_name} - {record.fecha_fichaje}"

    @api.constrains('fecha_fichaje')
    def _check_fecha_fichaje(self):
        """Prevent attendance records with dates in the future."""
        for record in self:
            if record.fecha_fichaje and record.fecha_fichaje > fields.Datetime.now():
                raise ValidationError(
                    'La fecha de fichaje no puede ser posterior a la fecha actual.'
                )

    @api.constrains('employee_id')
    def _check_employee_is_current_user(self):
        """Prevent users from creating records for other employees
        unless they belong to the manager group."""
        for record in self:
            is_manager = self.env.user.has_group('base.group_system')
            if record.employee_id != self.env.user and not is_manager:
                raise ValidationError(
                    'No puede registrar fichajes para otro empleado.'
                )

    @api.model
    def registrar_fichaje(self, tipo='entrada'):
        """Create a clock-in record for the current user.

        Only the authenticated user's own record is created,
        enforced by the constraint above.
        """
        valid_tipos = [t[0] for t in self._fields['tipo_accion'].selection]
        if tipo not in valid_tipos:
            raise ValidationError(
                f"Tipo de acción no válido: '{tipo}'. "
                f"Opciones: {', '.join(valid_tipos)}"
            )
        return self.create({
            'employee_id': self.env.user.id,
            'fecha_fichaje': fields.Datetime.now(),
            'tipo_accion': tipo,
        })
